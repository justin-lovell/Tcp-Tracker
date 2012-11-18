using System;
using System.Net;
using System.Net.Sockets;

namespace TcpTracker
{
    public class TcpExchangeHub
    {
        private readonly ITcpDataLogger _logger;
        private Socket _clientSocket;

        public EndPoint ClientEndPoint
        {
            get { return this._clientSocket.RemoteEndPoint; }
        }

        public TcpExchangeHub(ITcpDataLogger logger)
        {
            this._logger = logger;
        }

        public void WireExchange(Socket relaySocket, Socket clientSocket)
        {
            var relayToClient = new Context(relaySocket, clientSocket, TcpRelayDirection.RelayToClient);
            var clientToRelay = new Context(clientSocket, relaySocket, TcpRelayDirection.ClientToRelay);

            this.WaitToReceiveData(relayToClient);
            this.WaitToReceiveData(clientToRelay);

            this._clientSocket = clientSocket;
        }

        private void WaitToReceiveData(Context context)
        {
            if (context.IncomingSocket.Connected)
            {
                context.IncomingSocket.BeginReceive(context.Buffer, 0, context.Buffer.Length,
                                                    SocketFlags.None, this.IncomingSocketEndReceive, context);
            }
        }

        private void IncomingSocketEndReceive(IAsyncResult ar)
        {
            var context = (Context)ar.AsyncState;

            if (!context.IncomingSocket.Connected)
            {
                return;
            }

            int bytesRead = context.IncomingSocket.EndReceive(ar);

            if (bytesRead == 0)
            {
                this._logger.Disconnected(this, context.Direction);

                if (context.OutgoingSocket.Connected)
                {
                    context.OutgoingSocket.BeginDisconnect(false,
                                                           context.OutgoingSocket.EndDisconnect,
                                                           context
                        );
                }

                return;
            }

            if (!context.OutgoingSocket.Connected)
            {
                return;
            }

            this._logger.TransmitData(this, context.Direction, context.Buffer, bytesRead);
            context.OutgoingSocket.BeginSend(context.Buffer, 0, bytesRead,
                                             SocketFlags.None, this.OutboundSocketEndSend, context);
        }

        private void OutboundSocketEndSend(IAsyncResult ar)
        {
            var context = (Context)ar.AsyncState;

            context.OutgoingSocket.EndSend(ar);
            this.WaitToReceiveData(context);
        }

        private sealed class Context
        {
            public readonly Socket IncomingSocket;
            public readonly Socket OutgoingSocket;
            public readonly TcpRelayDirection Direction;

            public readonly byte[] Buffer = new byte[0x10];

            public Context(Socket incomingSocket, Socket outgoingSocket, TcpRelayDirection direction)
            {
                this.IncomingSocket = incomingSocket;
                this.OutgoingSocket = outgoingSocket;
                this.Direction = direction;
            }
        }
    }
}