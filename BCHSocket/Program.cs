/*
 * Copyright (c) 2019 ProtocolCash
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 *
 */

using System;
using System.Collections.Generic;
using SharpBCH.Node;
using System.Linq;
using System.Net;
using BCHSocket.Consumer;
using BCHSocket.Subscriptions;
using BCHSocket.Websocket;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SharpBCH;
using SharpBCH.Block;
using SharpBCH.CashAddress;
using SharpBCH.Transaction;
using SharpBCH.Util;

namespace BCHSocket
{
    public class Program
    {
        private static WebsocketServer _server;
        private static SubscriptionHandler _subscriptionHandler;

        public static void Main(string[] args)
        {
            // start with a ZMQ connection for blocks
            var blockConsumer = new BlockConsumer(HandleBlock);
            var zmqBlockClient = new ZMQClient(Config.GetConfigString("ZMQBlockHostname"), 
                Config.GetConfigInt("ZMQBlockPort"), "rawblock",
                (frameHeader, data, frameCounter) => blockConsumer.EnqueueTask(data, frameCounter));

            // start a ZMQ subscription for transactions
            var transactionConsumer = new TransactionConsumer(HandleTransaction);
            var zmqTxClient = new ZMQClient(Config.GetConfigString("ZMQTxHostname"),
                Config.GetConfigInt("ZMQTxPort"), "rawtx",
                (frameHeader, data, frameCounter) => transactionConsumer.EnqueueTask(data, frameCounter));

            // get the subscription handler ready
            _subscriptionHandler = new SubscriptionHandler();

            // initialize websocket server
            // TODO: we need an x509 certificate including key to use secure (wss) - add to application config options
            _server = new WebsocketServer(IPAddress.Parse(Config.GetConfigString("WebsocketBindIP")),
                Config.GetConfigInt("WebsocketBindPort"), false, false)
            // restart websocket listener automatically
            { RestartAfterListenError = true };

            // start the websocket server, and add callbacks to track active sockets
            _server.Start(socket =>
            {
                socket.OnOpen = () =>
                {
                    _subscriptionHandler.AddSocket(socket);
                };
                socket.OnClose = () =>
                {
                    _subscriptionHandler.RemoveSocket(socket);
                };
                socket.OnMessage = message =>
                {
                    HandleMessage(socket, message);
                };
                socket.OnError = exception =>
                {
                    // TODO: handle websocket exceptions
                };
            });
        }

        /// <summary>
        ///     Handles json messages:
        ///         {"op": "block"}
        ///         {"op": "address", "address": "[CASH_ADDRESS]"}
        ///         {"op": "opreturn", "prefix": "[PREFIX(HEX)]"}
        ///         {"op": "rm_block"}
        ///         {"op": "rm_address", "address": "[CASH_ADDRESS]"}
        ///         {"op": "rm_opreturn", "prefix": "[PREFIX(HEX)]"}
        ///     - Validates json
        ///     - Validates parameters
        ///     - Sends response (error or ok)
        ///     - Adds valid subscriptions to subscription handler
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="message"></param>
        private static void HandleMessage(IWebsocketConnection socket, string message)
        {
            message = message.Trim();

            // check if the message is valid json
            if ((!message.StartsWith("{") || !message.EndsWith("}")) &&
                (!message.StartsWith("[") || !message.EndsWith("]"))) return;

            JToken jToken;
            try
            {
                jToken = JToken.Parse(message);
            }
            catch (JsonReaderException jex)
            {
                socket.Send("{ \"op\": \"error\", \"error\": \"Unable to parse JSON request. " + JsonConvert.ToString(jex.Message) + "\" }");
                return;
            }
            catch (Exception ex) 
            {
                socket.Send("{ \"op\": \"error\", \"error\": \"Exception while parsing JSON request. " + JsonConvert.ToString(ex.Message) + "\" }");
                return;
            }

            if (jToken.Type != JTokenType.Object)
            {
                socket.Send("{ \"op\": \"error\", \"error\": \"Error while parsing JSON request. Expected Object, but encountered " + jToken.Type + "\" }");
                return;
            }

            var jObject = jToken.ToObject<JObject>();

            if (!jObject.ContainsKey("op") || jObject["op"].Type != JTokenType.String)
            {
                socket.Send("{ \"op\": \"error\", \"error\": \"Error while parsing JSON request. Expected 'op' parameter as string.\" }");
                return;
            }

            // block subscriptions
            if (jObject["op"].ToString().Equals("block",StringComparison.CurrentCultureIgnoreCase))
            {
                _subscriptionHandler.AddSubscription(socket, new BlockSubscription());
                socket.Send("{ \"op\": \"block\", \"result\": \"ok\" }");
            }
            // block un-subscribe
            else if (jObject["op"].ToString().Equals("rm_block", StringComparison.CurrentCultureIgnoreCase))
            {
                socket.Send(_subscriptionHandler.RemoveSubscription(socket, new BlockSubscription())
                    ? "{ \"op\": \"rm_block\", \"result\": \"ok\" }"
                    : "{ \"op\": \"rm_block\", \"result\": \"failed\" }");
            }

            // address subscriptions
            else if (jObject["op"].ToString().Equals("address", StringComparison.CurrentCultureIgnoreCase) 
                     || jObject["op"].ToString().Equals("rm_address", StringComparison.CurrentCultureIgnoreCase))
            {
                if (!jObject.ContainsKey("address") || jObject["address"].Type != JTokenType.String)
                {
                    socket.Send("{ \"op\": \"error\", \"error\": \"Error while parsing JSON request. Expected 'address' parameter as string.\" }");
                    return;
                }

                // attempt to decode cash address
                try
                {
                    var decoded = CashAddress.DecodeCashAddress(jObject["address"].ToString());

                    if (jObject["op"].ToString().Equals("address", StringComparison.CurrentCultureIgnoreCase))
                    {
                        _subscriptionHandler.AddSubscription(socket, new AddressSubscription(decoded));
                        socket.Send("{ \"op\": \"address\", \"result\": \"ok\" }");
                    }

                    else if (jObject["op"].ToString().Equals("rm_address", StringComparison.CurrentCultureIgnoreCase))
                        socket.Send(!_subscriptionHandler.RemoveSubscription(socket, new AddressSubscription(decoded))
                            ? "{ \"op\": \"rm_address\", \"result\": \"failed\" }"
                            : "{ \"op\": \"rm_address\", \"result\": \"ok\" }");
                }
                catch (CashAddress.CashAddressException e)
                {
                    socket.Send("{ \"op\": \"error\", \"error\": \"" + JsonConvert.ToString(e.Message) + " ... " + JsonConvert.ToString(e.InnerException.Message) + "\" }");
                }
            }

            // op return subscriptions
            else if (jObject["op"].ToString().Equals("opreturn", StringComparison.CurrentCultureIgnoreCase) ||
                     jObject["op"].ToString().Equals("rm_opreturn", StringComparison.CurrentCultureIgnoreCase))
            {
                if (!jObject.ContainsKey("prefix") || jObject["prefix"].Type != JTokenType.String)
                {
                    socket.Send("{ \"op\": \"error\", \"error\": \"Error while parsing JSON request. Expected 'prefix' parameter as string.\" }");
                    return;
                }
                if (!IsValidHexString(jObject["prefix"].ToString()))
                {
                    socket.Send("{ \"op\": \"error\", \"error\": \"Error while parsing JSON request. Expected 'prefix' parameter to be valid hex.\" }");
                    return;
                }
                if (jObject["prefix"].ToString().Length > 32)
                {
                    socket.Send("{ \"op\": \"error\", \"error\": \"Error while parsing JSON request. Expected 'prefix' parameter to be less than 32 hex characters long.\" }");
                    return;
                }

                var prefix = ByteHexConverter.StringToByteArray(jObject["prefix"].ToString());

                if (jObject["op"].ToString().Equals("opreturn", StringComparison.CurrentCultureIgnoreCase))
                {
                    _subscriptionHandler.AddSubscription(socket, new OpReturnSubscription(prefix));
                    socket.Send("{ \"op\": \"opreturn\", \"result\": \"ok\" }");
                }
                else if (_subscriptionHandler.RemoveSubscription(socket,
                    new OpReturnSubscription(prefix)))
                {
                    socket.Send(!_subscriptionHandler.RemoveSubscription(socket, new OpReturnSubscription(prefix))
                        ? "{ \"op\": \"rm_opreturn\", \"result\": \"failed\" }"
                        : "{ \"op\": \"rm_opreturn\", \"result\": \"ok\" }");
                }
            }

            else
            {
                // unrecognized op command
                socket.Send("{ \"op\": \"error\", \"error\": \"Error while parsing JSON request. Expected valid 'op' parameter.\" }");
            }
        }


        public static bool IsValidHexString(string hexString)
        {
            return hexString.Select(currentCharacter =>
                currentCharacter >= '0' && currentCharacter <= '9' ||
                currentCharacter >= 'a' && currentCharacter <= 'f' ||
                currentCharacter >= 'A' && currentCharacter <= 'F').All(isHexCharacter => isHexCharacter);
        }

        private static void HandleBlock(Block block)
        {
            // get websocket connections that are subscribed to blocks
            var subscribedSockets = _subscriptionHandler.GetSocketsWithSubscription(new BlockSubscription()).ToArray();

            Console.WriteLine("Broadcasting new block announcement to " + subscribedSockets.Length + " websocket clients:");
            Console.WriteLine("BlockHash: " + block.BlockHash + ". PrevBlockHash: " + block.Header.PrevBlockHash + ". Transactions: " + block.Transactions.Length);

            // broadcast new block announcements to all subscribed websocket clients
            foreach (var socket in subscribedSockets.ToList())
            {
                try
                {
                    socket.Send("{ \"op\": \"new_block\", \"blockHash\": \"" + block.BlockHash + "\", " +
                                "\"prevBlockHash\": \"" + block.Header.PrevBlockHash + "\", \"transactions\": " +
                                block.Transactions.Length + " }");
                }
                catch (Exception e)
                {
                    Console.WriteLine("Failed to broadcast. " + e.Message);
                }
            }
        }

        private static void HandleTransaction(Transaction transaction)
        {
            var subscribedSockets = new List<IWebsocketConnection>();
            // check each output for op_return and address subscriptions
            foreach (var output in transaction.Outputs)
            {
                if (output.Type == ScriptType.P2PKH || output.Type == ScriptType.P2SH)
                    subscribedSockets.AddRange(_subscriptionHandler
                        .GetSocketsWithSubscription(new AddressSubscription(
                            new DecodedBitcoinAddress("bitcoincash", output.Type, output.GetHash160()))));

                if (output.Type == ScriptType.DATA)
                    subscribedSockets.AddRange(_subscriptionHandler
                        .GetSocketsWithSubscription(new OpReturnSubscription(output.GetOpReturnData(16))));
            }

            Console.WriteLine("Transaction Detected! TXID: " + transaction.TXIDHex + ". Inputs: " + transaction.Inputs.Length + ". Outputs: " + transaction.Outputs.Length);
            // TODO: do subscription check on the transaction outputs and broadcast to subscribed sockets

            foreach (var socket in subscribedSockets)
            {
                BroadcastTransaction(transaction, socket);
            }
        }

        private static void BroadcastTransaction(Transaction transaction, IWebsocketConnection socket)
        {
            try
            {
                var outputs = transaction.Outputs.Aggregate(", ", (current, output) =>
                    current + "{ \"type\": \""+output.Type.ToString()+"\", \"address\": \"" + output.Address + "\", \"value\": \"" + output.Value + "\", \"script\": \"" + output.ScriptDataHex + "\" }");

                socket.Send("{ \"op\": \"new_tx\", \"txid\": \"" + transaction.TXIDHex + "\", " +
                            "\"inputs\": " + transaction.Inputs.Length + ", \"outputs\": [ " +
                            outputs + " ]");
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed to broadcast. " + e.Message);
            }
        }
    }
}
