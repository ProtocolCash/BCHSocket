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

using System.Linq;
using SharpBCH.Util;

namespace BCHSocket.Subscriptions
{    
    /// <summary>
    ///     Represents a broadcast subscription for transactions with an op_return output matching the given prefix
    /// </summary>
    public class OpReturnSubscription : Subscription
    {
        /// <summary>
        ///     Prefix to watch
        /// </summary>
        public byte[] OpReturnPrefix { get; }

        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="prefix">prefix to watch</param>
        public OpReturnSubscription(byte[] prefix) : base(SubscriptionType.opreturn)
        {
            OpReturnPrefix = prefix;
        }

        /// <summary>
        ///     Compares two Subscription objects
        ///     - different if type differs, or prefix is not present in given obj
        /// </summary>
        /// <param name="obj">output to examine for matching prefix</param>
        /// <returns></returns>
        public override int CompareTo(object obj)
        {
            // different if subscription type is not the same
            if (obj.GetType() != typeof(OpReturnSubscription))
                return string.CompareOrdinal(obj.GetType().Name, typeof(OpReturnSubscription).Name);

            var compare = (OpReturnSubscription) obj;

            // check if the given prefix is present in the data
            if (ByteUtil.IsByteArrayPrefix(compare.OpReturnPrefix, OpReturnPrefix))
                return 0;

            return ByteUtil.CompareByteArray(compare.OpReturnPrefix.SkipLast(compare.OpReturnPrefix.Length - OpReturnPrefix.Length).ToArray(), OpReturnPrefix);
        }
    }
}