using System;
using System.Collections.Generic;
using System.Linq;

namespace BCHSocket.Websocket
{
    public class SubProtocolNegotiationFailureException : Exception
    {
        public SubProtocolNegotiationFailureException(string message) : base(message) { }
    }

    public static class SubProtocolNegotiator
    {
        public static string Negotiate(IEnumerable<string> server, IEnumerable<string> client)
        {
            var enumerable = server.ToArray();
            var enumerable1 = client.ToArray();
            if (!enumerable.Any() || !enumerable1.Any())
                return null;

            var matches = enumerable1.Intersect(enumerable);
            var matches1 = matches as string[] ?? matches.ToArray();
            if (!matches1.Any())
                throw new SubProtocolNegotiationFailureException("Unable to negotiate a sub protocol.");

            return matches1.First();
        }
    }
}