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
using SharpBCH.Block;

namespace BCHSocket.Consumer
{
    public class BlockConsumer : Consumer<byte[]>
    {
        /// <summary>
        ///     Processes raw bitcoin block
        ///     - decodes the block, transactions, output scripts, and output addresses
        /// </summary>
        /// <param name="data">raw block byte array</param>
        public override void DoWork(byte[] data)
        {
            Block block;
            try
            {
                block = new Block(data);
            }
            catch (Exception e)
            {
                Console.WriteLine("Unable to decode block! " + e.Message);
                return;
            }

            Console.WriteLine("Received Block with " + block.Transactions.Length + " transactions.\n" +
                              " Previous Block: " + block.Header.PrevBlockHash + "\n" +
                              " Block Hash: " + block.BlockHash);

            // check all transactions
            foreach (var transaction in block.Transactions)
            {   
                // TODO
            }
        }
    }
}
