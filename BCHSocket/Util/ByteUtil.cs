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

namespace BCHSocket.Util
{
    /// <summary>
    ///     (Probably over-) optimized Byte comparison utilities
    /// </summary>
    public static unsafe class ByteUtil
    {
        /// <summary>
        ///     Compare two byte arrays
        /// </summary>
        /// <param name="a1">byte array 1</param>
        /// <param name="a2">byte array 2</param>
        /// <returns>0 if the same, 1 if a1 is larger, -1 is a2 is larger</returns>
        public static int CompareByteArray(byte[] a1, byte[] a2)
        {
            if (a1 == a2) return 0;
            if (a1 == null || a1.Length > a2.Length)
                return 1;
            if (a2.Length > a1.Length)
                return -1;

            fixed (byte* p1 = a1, p2 = a2)
            {
                byte* x1 = p1, x2 = p2;
                var l = a1.Length;
                for (var i = 0; i < l / 8; i++, x1 += 8, x2 += 8)
                {
                    if (*(long*)x1 > *(long*)x2) return 1;
                    if (*(long*)x1 < *(long*)x2) return -1;
                }

                if ((l & 4) != 0)
                {
                    if (*(int*)x1 > *(int*)x2)
                        return 1;
                    if (*(int*)x1 < *(int*)x2)
                        return -1;
                    x1 += 4;
                    x2 += 4;
                }

                if ((l & 2) != 0)
                {
                    if (*(short*)x1 > *(short*)x2)
                        return 1;
                    if (*(short*)x1 < *(short*)x2)
                        return -1;
                    x1 += 2;
                    x2 += 2;
                }

                if ((l & 1) == 0) return 0;

                if (*x1 > *x2)
                    return 1;
                if (*x1 < *x2)
                    return -1;

                return 0;
            }
        }

        /// <summary>
        ///     Checks if the given prefix exists at the start of the given data
        /// </summary>
        /// <param name="data">byte array in which to search</param>
        /// <param name="prefix">prefix to search for at the start of data</param>
        /// <returns>true is prefix exists, false otherwise</returns>
        public static bool IsByteArrayPrefix(byte[] data, byte[] prefix)
        {
            if (prefix == data) return true;
            if (prefix == null || prefix.Length > data.Length)
                return false;
            // if data is longer then prefix, trim data to be same as prefix
            if (data.Length > prefix.Length)
                data = data.SkipLast(data.Length - prefix.Length).ToArray();

            fixed (byte* p1 = prefix, p2 = data)
            {
                byte* x1 = p1, x2 = p2;
                var l = prefix.Length;
                for (var i = 0; i < l / 8; i++, x1 += 8, x2 += 8)
                    if (*(long*)x1 != *(long*)x2) return false;

                if ((l & 4) != 0)
                {
                    if (*(int*)x1 != *(int*)x2)
                        return false;
                    x1 += 4;
                    x2 += 4;
                }

                if ((l & 2) != 0)
                {
                    if (*(short*)x1 != *(short*)x2)
                        return false;
                    x1 += 2;
                    x2 += 2;
                }

                if ((l & 1) == 0) return true;

                return *x1 == *x2;
            }
        }
    }
}