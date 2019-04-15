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
using SharpBCH.Transaction;
using SharpBCH.Util;

namespace BCHSocket.Consumer
{
    /// <inheritdoc />
    /// <summary>
    ///     Implements a consumer thread that decodes raw bitcoin transactions
    /// </summary>
    public class TransactionConsumer : Consumer<byte[], Transaction>
    {
        public TransactionConsumer(Action<Transaction> callbackAction) : base(callbackAction)
        {
        }

        /// <summary>
        ///     Processes raw bitcoin transactions
        ///     - decodes the transaction, output scripts, and output addresses
        /// </summary>
        /// <param name="data">raw transaction byte array</param>
        protected override Transaction DoWork(byte[] data)
        {
            // hex version of the transaction
            var txHex = ByteHexConverter.ByteArrayToHex(data);

            // attempt to decode the transaction (also decodes output scripts and addresses)
            Transaction transaction;
            try
            {
                transaction = new Transaction(data);
            }
            catch (Exception e)
            {
                Console.WriteLine("Unable to decode transaction: \n" + txHex + "\n" + e.Message);
                return null;
            }

            return transaction;
        }
    }
}