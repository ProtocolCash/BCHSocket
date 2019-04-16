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
using BCHSocket.Util;
using SharpBCH.CashAddress;

namespace BCHSocket.Subscriptions
{
    /// <summary>
    ///     Represents a broadcast subscription for transactions with a given output addresses
    /// </summary>
    public class AddressSubscription : Subscription
    {
        /// <summary>
        ///     Decoded address to watch
        /// </summary>
        public DecodedBitcoinAddress DecodedAddress { get; }

        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="decodedAddress">Decoded address to watch</param>
        public AddressSubscription(DecodedBitcoinAddress decodedAddress) : base(SubscriptionType.address)
        {
            DecodedAddress = decodedAddress;
        }

        /// <summary>
        ///     Compares two Subscription objects
        ///     - different if type, prefix, or hash differ
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override int CompareTo(object obj)
        {
            // different if subscription type is not the same
            if (obj.GetType() != typeof(AddressSubscription))
                return string.Compare(obj.GetType().Name, typeof(AddressSubscription).Name, StringComparison.Ordinal);

            var compare = (AddressSubscription) obj;

            // different if address type is not the same
            if (compare.DecodedAddress.Type != DecodedAddress.Type)
                return string.Compare(compare.DecodedAddress.Type.ToString(), DecodedAddress.Type.ToString(), StringComparison.Ordinal);

            // different if prefix is not the same
            if (compare.DecodedAddress.Prefix != DecodedAddress.Prefix)
                return string.Compare(compare.DecodedAddress.Prefix, DecodedAddress.Prefix, StringComparison.Ordinal);

            // type and prefix are the same; so compare the hash
            return ByteUtil.CompareByteArray(compare.DecodedAddress.Hash, DecodedAddress.Hash);
        }
    }
}