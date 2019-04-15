using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace BCHSocket.Websocket
{
    public class WebsocketConnection : IWebsocketConnection
    {
        public WebsocketConnection(SocketWrapper socket, Action<IWebsocketConnection> callbacks, Func<byte[], WebsocketHttpRequest> parseRequest, Func<WebsocketHttpRequest, IHandler> handlerFactory, Func<IEnumerable<string>, string> negotiateSubProtocol)
        {
            Socket = socket;
            OnOpen = () => { };
            OnClose = () => { };
            OnMessage = x => { };
            OnBinary = x => { };
            OnPing = x => SendPong(x);
            OnPong = x => { };
            OnError = x => { };
            _handlerFactory = handlerFactory;
            _callbacks = callbacks;
            _parseRequest = parseRequest;
            _negotiateSubProtocol = negotiateSubProtocol;
        }

        public SocketWrapper Socket { get; set; }

        private readonly Func<WebsocketHttpRequest, IHandler> _handlerFactory;
        private readonly Action<WebsocketConnection> _callbacks;
        private readonly Func<IEnumerable<string>, string> _negotiateSubProtocol;
        readonly Func<byte[], WebsocketHttpRequest> _parseRequest;

        public IHandler Handler { get; set; }

        private bool _closing;
        private bool _closed;
        private const int ReadSize = 1024 * 4;

        public Action OnOpen { get; set; }

        public Action OnClose { get; set; }

        public Action<string> OnMessage { get; set; }

        public Action<byte[]> OnBinary { get; set; }

        public Action<byte[]> OnPing { get; set; }

        public Action<byte[]> OnPong { get; set; }

        public Action<Exception> OnError { get; set; }

        public WebsocketConnectionInfo ConnectionInfo { get; private set; }

        public bool IsAvailable => !_closing && !_closed && Socket.Connected;

        public Task Send(string message)
        {
            return Send(message, Handler.FrameText);
        }

        public Task Send(byte[] message)
        {
            return Send(message, Handler.FrameBinary);
        }

        public Task SendPing(byte[] message)
        {
            return Send(message, Handler.FramePing);
        }

        public Task SendPong(byte[] message)
        {
            return Send(message, Handler.FramePong);
        }

        private Task Send<T>(T message, Func<T, byte[]> createFrame)
        {
            if (Handler == null)
                throw new InvalidOperationException("Cannot send before handshake");

            if (!IsAvailable)
            {
                const string errorMessage = "Data sent while closing or after close. Ignoring.";
                Console.WriteLine(errorMessage);

                var taskForException = new TaskCompletionSource<object>();
                taskForException.SetException(new WebException(errorMessage));
                return taskForException.Task;
            }

            var bytes = createFrame(message);
            return SendBytes(bytes);
        }

        public void StartReceiving()
        {
            var data = new List<byte>(ReadSize);
            var buffer = new byte[ReadSize];
            Read(data, buffer);
        }

        public void Close()
        {
            Close(WebsocketStatusCodes.NormalClosure);
        }

        public void Close(int code)
        {
            if (!IsAvailable)
                return;

            _closing = true;

            if (Handler == null)
            {
                CloseSocket();
                return;
            }

            var bytes = Handler.FrameClose(code);
            if (bytes.Length == 0)
                CloseSocket();
            else
                SendBytes(bytes, CloseSocket);
        }

        public void CreateHandler(IEnumerable<byte> data)
        {
            var request = _parseRequest(data.ToArray());
            if (request == null)
                return;
            Handler = _handlerFactory(request);
            if (Handler == null)
                return;
            var subProtocol = _negotiateSubProtocol(request.SubProtocols);
            ConnectionInfo = WebsocketConnectionInfo.Create(request, Socket.RemoteIpAddress, Socket.RemotePort, subProtocol);

            _callbacks(this);

            var handshake = Handler.CreateHandshake(subProtocol);
            SendBytes(handshake, OnOpen);
        }

        private void Read(List<byte> data, byte[] buffer)
        {
            if (!IsAvailable)
                return;

            Socket.Receive(buffer, r =>
            {
                if (r <= 0)
                {
                    Console.WriteLine("0 bytes read. Closing.");
                    CloseSocket();
                    return;
                }
                // FleckLog.Debug(r + " bytes read");
                var readBytes = buffer.Take(r);
                if (Handler != null)
                {
                    Handler.Receive(readBytes);
                }
                else
                {
                    data.AddRange(readBytes);
                    CreateHandler(data);
                }

                Read(data, buffer);
            },
            HandleReadError);
        }

        private void HandleReadError(Exception e)
        {
            while (true)
            {
                switch (e)
                {
                    case AggregateException exception:
                    {
                        var agg = exception;
                        e = agg.InnerException;
                        continue;
                    }
                    case ObjectDisposedException _:
                        Console.WriteLine("Swallowing ObjectDisposedException. " + e.Message);
                        return;
                }

                OnError(e);

                switch (e)
                {
                    case WebsocketException exception:
                        Console.WriteLine("Error while reading. " + exception.Message);
                        Close(exception.StatusCode);
                        break;
                    case IOException _:
                        Console.WriteLine("Error while reading. " + e.Message);
                        Close(WebsocketStatusCodes.AbnormalClosure);
                        break;
                    default:
                        Console.WriteLine("Application Error. " + e.Message);
                        Close(WebsocketStatusCodes.InternalServerError);
                        break;
                }

                break;
            }
        }

        private Task SendBytes(byte[] bytes, Action callback = null)
        {
            return Socket.Send(bytes, () =>
                {
                    //FleckLog.Debug("Sent " + bytes.Length + " bytes");
                    callback?.Invoke();
                },
                e =>
                {
                    if (e is IOException)
                        Console.WriteLine("Failed to send. Disconnecting. " + e.Message);
                    else
                        Console.WriteLine("Failed to send. Disconnecting. " + e.Message);
                    CloseSocket();
                });
        }

        private void CloseSocket()
        {
            _closing = true;
            OnClose();
            _closed = true;
            Socket.Close();
            Socket.Dispose();
            _closing = false;
        }

    }
}