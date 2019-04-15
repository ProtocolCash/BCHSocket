using System;

namespace BCHSocket.Websocket
{
    public class WebsocketException : Exception
    {
        public WebsocketException(ushort statusCode) : base()
        {
            StatusCode = statusCode;
        }

        public WebsocketException(ushort statusCode, string message) : base(message)
        {
            StatusCode = statusCode;
        }

        public WebsocketException(ushort statusCode, string message, Exception innerException) : base(message, innerException)
        {
            StatusCode = statusCode;
        }

        public ushort StatusCode { get; private set; }
    }
}