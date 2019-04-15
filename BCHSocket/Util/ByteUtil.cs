using System.Linq;

namespace BCHSocket.Util
{
    public static class ByteUtil
    {
        public static unsafe int CompareByteArray(byte[] a1, byte[] a2)
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
                    if (*((int*)x1) > *((int*)x2))
                        return 1;
                    if (*((int*)x1) < *((int*)x2))
                        return -1;
                    x1 += 4;
                    x2 += 4;
                }

                if ((l & 2) != 0)
                {
                    if (*((short*)x1) > *((short*)x2))
                        return 1;
                    if (*((short*)x1) < *((short*)x2))
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

        public static unsafe bool IsByteArrayPrefix(byte[] data, byte[] prefix)
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
                    if (*((int*)x1) != *((int*)x2))
                        return false;
                    x1 += 4;
                    x2 += 4;
                }

                if ((l & 2) != 0)
                {
                    if (*((short*)x1) != *((short*)x2))
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