using System;
using System.Text;

namespace BCHSocket.Websocket.Handlers
{
    public class FlashSocketPolicyRequestHandler
    {
        public static string PolicyResponse =
            "<?xml version=\"1.0\"?>\n" +
            "<cross-domain-policy>\n" +
            "   <allow-access-from domain=\"*\" to-ports=\"*\"/>\n" +
            "   <site-control permitted-cross-domain-policies=\"all\"/>\n" +
            "</cross-domain-policy>\n" +
            "\0";

        public static IHandler Create(WebsocketHttpRequest request)
        {
            return new ComposableHandler
            {
                Handshake = sub => Handshake(request, sub),
            };
        }

        public static byte[] Handshake(WebsocketHttpRequest request, string subProtocol)
        {
            // Console.WriteLine("Building Flash Socket Policy Response");
            return Encoding.UTF8.GetBytes(PolicyResponse);
        }
    }
}