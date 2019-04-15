using System;
using System.Collections.Generic;

namespace BCHSocket.Websocket
{
    public class WebsocketHttpRequest
    {
        public string Method { get; set; }

        public string Path { get; set; }

        public string Body { get; set; }

        public string Scheme { get; set; }

        public byte[] Bytes { get; set; }

        public string this[string name] => Headers.TryGetValue(name, out var value) ? value : default(string);

        public IDictionary<string, string> Headers { get; } = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);

        public string[] SubProtocols =>
            Headers.TryGetValue("Sec-WebSocket-Protocol", out var value)
                ? value.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                : new string[0];
    }
}