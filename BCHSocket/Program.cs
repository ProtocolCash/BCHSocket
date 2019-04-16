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

using SharpBCH.Node;
using System.Net;
using BCHSocket.Consumer;
using BCHSocket.Websocket;

namespace BCHSocket
{
    public class Program
    {
        private static WebsocketServer _server;
        private static SubscriptionHandler _subscriptionHandler;
        private static DataHandler _dataHandler;

        public static void Main(string[] args)
        {
            // get the subscription handler ready (tracks websocket clients and their subscriptions)
            _subscriptionHandler = new SubscriptionHandler();
            // get the dataHandler ready (handles incoming blocks and transactions, compares to client subscriptions using the subscription handler)
            _dataHandler = new DataHandler(_subscriptionHandler);

            // start with a ZMQ connection for blocks
            var blockConsumer = new BlockConsumer(_dataHandler.HandleBlock);
            var zmqBlockClient = new ZMQClient(Config.GetConfigString("ZMQBlockHostname"), 
                Config.GetConfigInt("ZMQBlockPort"), "rawblock",
                (frameHeader, data, frameCounter) => blockConsumer.EnqueueTask(data, frameCounter));

            // start a ZMQ subscription for transactions
            var transactionConsumer = new TransactionConsumer(_dataHandler.HandleTransaction);
            var zmqTxClient = new ZMQClient(Config.GetConfigString("ZMQTxHostname"),
                Config.GetConfigInt("ZMQTxPort"), "rawtx",
                (frameHeader, data, frameCounter) => transactionConsumer.EnqueueTask(data, frameCounter));

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
                    // track the connection
                    _subscriptionHandler.AddSocket(socket);
                };
                socket.OnClose = () =>
                {
                    // stop tracking this connection
                    _subscriptionHandler.RemoveSocket(socket);
                };
                socket.OnMessage = message =>
                {
                    // process incoming message
                    MessageHandler.HandleMessage(socket, message, _subscriptionHandler);
                };
                socket.OnError = exception =>
                {
                    // TODO: handle websocket exceptions
                };
            });
        }

        
    }
}
