using System;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace BCHSocket.Websocket
{
    public class SocketWrapper
    {
        public const uint KeepAliveInterval = 60000;
        public const uint RetryInterval = 10000;

        private readonly Socket _socket;
        private readonly CancellationTokenSource _tokenSource;
        private readonly TaskFactory _taskFactory;

        public string RemoteIpAddress
        {
            get
            {
                var endpoint = _socket.RemoteEndPoint as IPEndPoint;
                return endpoint?.Address.ToString();
            }
        }

        public int RemotePort
        {
            get
            {
                var endpoint = _socket.RemoteEndPoint as IPEndPoint;
                return endpoint?.Port ?? -1;
            }
        }

        public void SetKeepAlive(Socket socket, uint keepAliveInterval, uint retryInterval)
        {
            const int size = sizeof(uint);
            const uint on = 1;

            var inArray = new byte[size * 3];
            Array.Copy(BitConverter.GetBytes(on), 0, inArray, 0, size);
            Array.Copy(BitConverter.GetBytes(keepAliveInterval), 0, inArray, size, size);
            Array.Copy(BitConverter.GetBytes(retryInterval), 0, inArray, size * 2, size);
            socket.IOControl(IOControlCode.KeepAliveValues, inArray, null);
        }

        public SocketWrapper(Socket socket)
        {
            _tokenSource = new CancellationTokenSource();
            _taskFactory = new TaskFactory(_tokenSource.Token);
            _socket = socket;
            if (_socket.Connected)
                Stream = new NetworkStream(_socket);

            // set tcp keep alive to something reasonable
            // TODO: does this work on MacOS and Linux?
            SetKeepAlive(socket, KeepAliveInterval, RetryInterval);
        }

        public Task Authenticate(X509Certificate2 certificate, SslProtocols enabledSslProtocols, Action callback, Action<Exception> error)
        {
            var ssl = new SslStream(Stream, false);
            Stream = new QueuedStream(ssl);

            IAsyncResult Begin(AsyncCallback cb, object s) => ssl.BeginAuthenticateAsServer(certificate, false, enabledSslProtocols, false, cb, s);

            var task = Task.Factory.FromAsync(Begin, ssl.EndAuthenticateAsServer, null);
            task.ContinueWith(t => callback(), TaskContinuationOptions.NotOnFaulted)
                .ContinueWith(t => error(t.Exception), TaskContinuationOptions.OnlyOnFaulted);
            task.ContinueWith(t => error(t.Exception), TaskContinuationOptions.OnlyOnFaulted);

            return task;
        }

        public void Listen(int backlog)
        {
            _socket.Listen(backlog);
        }

        public void Bind(EndPoint endPoint)
        {
            _socket.Bind(endPoint);
        }

        public bool Connected => _socket.Connected;

        public Stream Stream { get; private set; }

        public bool NoDelay
        {
            get => _socket.NoDelay;
            set => _socket.NoDelay = value;
        }

        public EndPoint LocalEndPoint => _socket.LocalEndPoint;

        public Task<int> Receive(byte[] buffer, Action<int> callback, Action<Exception> error, int offset = 0)
        {
            try
            {
                IAsyncResult Begin(AsyncCallback cb, object s) => Stream.BeginRead(buffer, offset, buffer.Length, cb, s);

                var task = Task.Factory.FromAsync(Begin, Stream.EndRead, null);
                task.ContinueWith(t => callback(t.Result), TaskContinuationOptions.NotOnFaulted)
                    .ContinueWith(t => error(t.Exception), TaskContinuationOptions.OnlyOnFaulted);
                task.ContinueWith(t => error(t.Exception), TaskContinuationOptions.OnlyOnFaulted);

                return task;
            }
            catch (Exception e)
            {
                error(e);
                return null;
            }
        }

        public Task<SocketWrapper> Accept(Action<SocketWrapper> callback, Action<Exception> error)
        {
            SocketWrapper End(IAsyncResult r) => _tokenSource.Token.IsCancellationRequested ? null : new SocketWrapper(_socket.EndAccept(r));

            var task = _taskFactory.FromAsync(_socket.BeginAccept, End, null);
            task.ContinueWith(t => callback(t.Result), TaskContinuationOptions.OnlyOnRanToCompletion)
                .ContinueWith(t => error(t.Exception), TaskContinuationOptions.OnlyOnFaulted);
            task.ContinueWith(t => error(t.Exception), TaskContinuationOptions.OnlyOnFaulted);

            return task;
        }

        public void Dispose()
        {
            _tokenSource.Cancel();
            Stream?.Dispose();
            _socket?.Dispose();
        }

        public void Close()
        {
            _tokenSource.Cancel();
            Stream?.Close();
            _socket?.Close();
        }

        public int EndSend(IAsyncResult asyncResult)
        {
            Stream.EndWrite(asyncResult);
            return 0;
        }

        public Task Send(byte[] buffer, Action callback, Action<Exception> error)
        {
            if (_tokenSource.IsCancellationRequested)
                return null;

            try
            {
                IAsyncResult Begin(AsyncCallback cb, object s) => Stream.BeginWrite(buffer, 0, buffer.Length, cb, s);

                var task = Task.Factory.FromAsync(Begin, Stream.EndWrite, null);
                task.ContinueWith(t => callback(), TaskContinuationOptions.NotOnFaulted)
                    .ContinueWith(t => error(t.Exception), TaskContinuationOptions.OnlyOnFaulted);
                task.ContinueWith(t => error(t.Exception), TaskContinuationOptions.OnlyOnFaulted);

                return task;
            }
            catch (Exception e)
            {
                error(e);
                return null;
            }
        }
    }
}