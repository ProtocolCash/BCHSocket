using System;
using System.Linq;
using BCHSocket.Subscriptions;
using BCHSocket.Websocket;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SharpBCH.CashAddress;
using SharpBCH.Util;

namespace BCHSocket
{
    public static class MessageHandler
    {
        /// <summary>
        ///     Handles json messages:
        ///         {"op": "block"}
        ///         {"op": "transaction"}
        ///         {"op": "address", "address": "[CASH_ADDRESS]"}
        ///         {"op": "opreturn", "prefix": "[PREFIX(HEX)]"}
        ///         {"op": "rm_block"}
        ///         {"op": "rm_transaction"}
        ///         {"op": "rm_address", "address": "[CASH_ADDRESS]"}
        ///         {"op": "rm_opreturn", "prefix": "[PREFIX(HEX)]"}
        ///     - Validates json
        ///     - Validates parameters
        ///     - Sends response (error or ok)
        ///     - Adds valid subscriptions to subscription handler
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="message"></param>
        /// <param name="subscriptionHandler"></param>
        public static void HandleMessage(IWebsocketConnection socket, string message, SubscriptionHandler subscriptionHandler)
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
            if (jObject["op"].ToString().Equals("block", StringComparison.CurrentCultureIgnoreCase))
            {
                subscriptionHandler.AddSubscription(socket, new BlockSubscription());
                socket.Send("{ \"op\": \"block\", \"result\": \"ok\" }");
            }
            // block un-subscribe
            else if (jObject["op"].ToString().Equals("rm_block", StringComparison.CurrentCultureIgnoreCase))
            {
                socket.Send(subscriptionHandler.RemoveSubscription(socket, new BlockSubscription())
                    ? "{ \"op\": \"rm_block\", \"result\": \"ok\" }"
                    : "{ \"op\": \"rm_block\", \"result\": \"failed\" }");
            }

            // transaction subscriptions
            else if (jObject["op"].ToString().Equals("transaction", StringComparison.CurrentCultureIgnoreCase))
            {
                subscriptionHandler.AddSubscription(socket, new TransactionSubscription());
                socket.Send("{ \"op\": \"transaction\", \"result\": \"ok\" }");
            }
            // transaction un-subscribe
            else if (jObject["op"].ToString().Equals("rm_transaction", StringComparison.CurrentCultureIgnoreCase))
            {
                socket.Send(subscriptionHandler.RemoveSubscription(socket, new TransactionSubscription())
                    ? "{ \"op\": \"rm_transaction\", \"result\": \"ok\" }"
                    : "{ \"op\": \"rm_transaction\", \"result\": \"failed\" }");
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
                        subscriptionHandler.AddSubscription(socket, new AddressSubscription(decoded));
                        socket.Send("{ \"op\": \"address\", \"result\": \"ok\" }");
                    }

                    else if (jObject["op"].ToString().Equals("rm_address", StringComparison.CurrentCultureIgnoreCase))
                        socket.Send(!subscriptionHandler.RemoveSubscription(socket, new AddressSubscription(decoded))
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
                    subscriptionHandler.AddSubscription(socket, new OpReturnSubscription(prefix));
                    socket.Send("{ \"op\": \"opreturn\", \"result\": \"ok\" }");
                }
                else if (subscriptionHandler.RemoveSubscription(socket,
                    new OpReturnSubscription(prefix)))
                {
                    socket.Send(!subscriptionHandler.RemoveSubscription(socket, new OpReturnSubscription(prefix))
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
    }
}