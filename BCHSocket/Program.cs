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
using System.Configuration;
using BCHSocket.Consumer;

namespace BCHSocket
{
    class Program
    {
        static void Main(string[] args)
        {
            // start with a ZMQ connection for blocks
            var zmqBlockHostname = ConfigurationManager.AppSettings["ZMQBlockHostname"];
            var zmqBlockPort = int.Parse(ConfigurationManager.AppSettings["ZMQBlockPort"]);
            var blockConsumer = new BlockConsumer();
            var zmqBlockClient = new ZMQClient(zmqBlockHostname, zmqBlockPort, "rawblock",
                (frameHeader, data, frameCounter) => blockConsumer.EnqueueTask(data, frameCounter));

            // start a ZMQ subscription for transactions
            var zmqTxHostname = ConfigurationManager.AppSettings["ZMQTxHostname"];
            var zmqTxPort = int.Parse(ConfigurationManager.AppSettings["ZMQTxPort"]);
            var transactionConsumer = new TransactionConsumer();
            var zmqTxClient = new ZMQClient(zmqTxHostname, zmqTxPort, "rawtx",
                (frameHeader, data, frameCounter) => transactionConsumer.EnqueueTask(data, frameCounter));
        }
    }
}
