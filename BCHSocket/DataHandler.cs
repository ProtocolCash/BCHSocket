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
using System.Linq;
using BCHSocket.Subscriptions;
using BCHSocket.Websocket;
using SharpBCH;
using SharpBCH.Block;
using SharpBCH.CashAddress;
using SharpBCH.Transaction;

namespace BCHSocket
{
    /// <summary>
    ///     Processes newly decoded blocks and transactions
    ///     - checks for existing websocket clients that are subscribed to events
    ///     - broadcasts update to any clients with matching subscriptions
    /// </summary>
    internal class DataHandler
    {
        private readonly SubscriptionHandler _subscriptionHandler;

        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="subscriptionHandler">initialized subscription handler</param>
        public DataHandler(SubscriptionHandler subscriptionHandler)
        {
            _subscriptionHandler = subscriptionHandler;
        }

        /// <summary>
        ///     Processes a decoded block
        /// </summary>
        /// <param name="block">decoded block</param>
        public void HandleBlock(Block block)
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

        /// <summary>
        ///     Processes a decoded transaction
        /// </summary>
        /// <param name="transaction">decoded transaction</param>
        public void HandleTransaction(Transaction transaction)
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

            subscribedSockets.AddRange(_subscriptionHandler.GetSocketsWithSubscription(new TransactionSubscription()));

            Console.WriteLine("Transaction Detected! TXID: " + transaction.TXIDHex + ". Inputs: " + transaction.Inputs.Length + 
                              ". Outputs: " + transaction.Outputs.Length + ". Broadcast to: " + subscribedSockets.Count + " clients.");

            foreach (var socket in subscribedSockets)
            {
                BroadcastTransaction(transaction, socket);
            }
        }

        /// <summary>
        ///     Broadcasts a decoded transaction to a given socket
        /// </summary>
        /// <param name="transaction">decoded transaction to broadcast</param>
        /// <param name="socket">socket to which to broadcast the transaction</param>
        private static void BroadcastTransaction(Transaction transaction, IWebsocketConnection socket)
        {
            try
            {
                var outputs = transaction.Outputs.Aggregate(", ", (current, output) =>
                    current + "{ \"type\": \"" + output.Type.ToString() + "\", \"address\": \"" + output.Address + "\", \"value\": \"" + output.Value + "\", \"script\": \"" + output.ScriptDataHex + "\" }");

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