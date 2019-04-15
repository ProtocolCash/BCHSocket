using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace BCHSocket.Websocket
{
    public class WebsocketServer
    {
        public Uri LocalUri { get; }
        public IPAddress BindIp { get; }
        public SocketWrapper ListenerSocket { get; private set; }
        public X509Certificate2 Certificate { get; set; }
        public SslProtocols EnabledSslProtocols { get; set; }
        private Action<IWebsocketConnection> _callbacks;
        public bool RestartAfterListenError { get; set; }
        public IEnumerable<string> SupportedSubProtocols { get; set; }

        public WebsocketServer(IPAddress bindIp, int port, bool secure = false, bool supportDualStack = true)
        {
            BindIp = bindIp;

            LocalUri = new UriBuilder(secure ? "wss" : "ws",bindIp.ToString(), port).Uri;

            var socket = new Socket(bindIp.AddressFamily, SocketType.Stream, ProtocolType.IP);

            if (supportDualStack)
            {
                // TODO: does this work on MacOS and Linux?
                socket.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, false);
                socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, 1);
            }

            ListenerSocket = new SocketWrapper(socket);
            SupportedSubProtocols = new string[0];
        }

        public void Start(Action<IWebsocketConnection> callsbacks)
        {
            _callbacks = callsbacks;

            var ipLocal = new IPEndPoint(BindIp, LocalUri.Port);
            ListenerSocket.Bind(ipLocal);
            ListenerSocket.Listen(100);

            Console.WriteLine("Websocket Server Started at " + LocalUri);

            if (LocalUri.Scheme == "wss")
            {
                if (Certificate == null)
                    throw new Exception("Cannot use secure mode 'wss' without a Certificate.");

                if (EnabledSslProtocols == SslProtocols.None)
                {
                    EnabledSslProtocols = SslProtocols.Tls;
                    Console.WriteLine("Using default TLS 1.0.");
                }
            }

            ListenForClients();
        }

        private void ListenForClients()
        {
            ListenerSocket.Accept(OnClientConnect, e => {
                Console.WriteLine("Listener socket is closed. " + e.Message);

                if (!RestartAfterListenError) return;

                Console.WriteLine("Listener socket restarting");
                try
                {
                    ListenerSocket.Dispose();
                    var socket = new Socket(BindIp.AddressFamily, SocketType.Stream, ProtocolType.IP);
                    ListenerSocket = new SocketWrapper(socket);
                    Start(_callbacks);
                    Console.WriteLine("Listener socket restarted");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Listener could not be restarted. " + ex.Message);
                }
            });
        }

        private void OnClientConnect(SocketWrapper clientSocket)
        {
            if (clientSocket == null) return; // socket closed

            Console.WriteLine("Client connected from " + clientSocket.RemoteIpAddress + ":" + clientSocket.RemotePort);

            // start listening for clients again immediately
            ListenForClients();

            WebsocketConnection connection = null;

            connection = new WebsocketConnection(
                clientSocket,
                _callbacks,
                bytes => RequestParser.Parse(bytes, LocalUri.Scheme),
                r => HandlerFactory.BuildHandler(r,
                    s => connection.OnMessage(s),
                    connection.Close,
                    b => connection.OnBinary(b),
                    b => connection.OnPing(b),
                    b => connection.OnPong(b)),
                s => SubProtocolNegotiator.Negotiate(SupportedSubProtocols, s));

            if (LocalUri.Scheme == "wss")
            {
                Console.WriteLine("Authenticating Secure Connection." );
                clientSocket
                    .Authenticate(Certificate,
                        EnabledSslProtocols,
                        connection.StartReceiving,
                        e => Console.WriteLine("Failed to Authenticate. " + e.Message));
            }
            else
            {
                connection.StartReceiving();
            }
        }
    }
}