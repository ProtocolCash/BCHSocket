﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace BCHSocket.Websocket.Handlers
{
    public static class Draft76Handler
    {
        private const byte End = 255;
        private const byte Start = 0;
        private const int MaxSize = 1024 * 1024 * 5;

        public static IHandler Create(WebsocketHttpRequest request, Action<string> onMessage)
        {
            return new ComposableHandler
            {
                TextFrame = FrameText,
                Handshake = sub => Handshake(request, sub),
                ReceiveData = data => ReceiveData(onMessage, data)
            };
        }

        public static void ReceiveData(Action<string> onMessage, List<byte> data)
        {
            while (data.Count > 0)
            {
                if (data[0] != Start)
                    throw new WebsocketException(WebsocketStatusCodes.InvalidFramePayloadData);

                var endIndex = data.IndexOf(End);
                if (endIndex < 0)
                    return;

                if (endIndex > MaxSize)
                    throw new WebsocketException(WebsocketStatusCodes.MessageTooBig);

                var bytes = data.Skip(1).Take(endIndex - 1).ToArray();

                data.RemoveRange(0, endIndex + 1);

                var message = Encoding.UTF8.GetString(bytes);

                onMessage(message);
            }
        }

        public static byte[] FrameText(string data)
        {
            var bytes = Encoding.UTF8.GetBytes(data);
            // wrap the array with the wrapper bytes
            var wrappedBytes = new byte[bytes.Length + 2];
            wrappedBytes[0] = Start;
            wrappedBytes[wrappedBytes.Length - 1] = End;
            Array.Copy(bytes, 0, wrappedBytes, 1, bytes.Length);
            return wrappedBytes;
        }

        public static byte[] Handshake(WebsocketHttpRequest request, string subProtocol)
        {
            Console.WriteLine("Building Draft76 Response");

            var builder = new StringBuilder();
            builder.Append("HTTP/1.1 101 WebSocket Protocol Handshake\r\n");
            builder.Append("Upgrade: WebSocket\r\n");
            builder.Append("Connection: Upgrade\r\n");
            builder.AppendFormat("Sec-WebSocket-Origin: {0}\r\n", request["Origin"]);
            builder.AppendFormat("Sec-WebSocket-Location: {0}://{1}{2}\r\n", request.Scheme, request["Host"], request.Path);

            if (subProtocol != null)
                builder.AppendFormat("Sec-WebSocket-Protocol: {0}\r\n", subProtocol);

            builder.Append("\r\n");

            var key1 = request["Sec-WebSocket-Key1"];
            var key2 = request["Sec-WebSocket-Key2"];
            var challenge = new ArraySegment<byte>(request.Bytes, request.Bytes.Length - 8, 8);

            var answerBytes = CalculateAnswerBytes(key1, key2, challenge);

            var byteResponse = Encoding.ASCII.GetBytes(builder.ToString());
            var byteResponseLength = byteResponse.Length;
            Array.Resize(ref byteResponse, byteResponseLength + answerBytes.Length);
            Array.Copy(answerBytes, 0, byteResponse, byteResponseLength, answerBytes.Length);

            return byteResponse;
        }

        public static byte[] CalculateAnswerBytes(string key1, string key2, ArraySegment<byte> challenge)
        {
            var result1Bytes = ParseKey(key1);
            var result2Bytes = ParseKey(key2);

            var rawAnswer = new byte[16];
            Array.Copy(result1Bytes, 0, rawAnswer, 0, 4);
            Array.Copy(result2Bytes, 0, rawAnswer, 4, 4);
            Array.Copy(challenge.Array, challenge.Offset, rawAnswer, 8, 8);

            return MD5.Create().ComputeHash(rawAnswer);
        }

        private static byte[] ParseKey(string key)
        {
            var spaces = key.Count(x => x == ' ');
            var digits = new string(key.Where(char.IsDigit).ToArray());

            var value = (int)(long.Parse(digits) / spaces);

            var result = BitConverter.GetBytes(value);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(result);
            return result;
        }
    }
}