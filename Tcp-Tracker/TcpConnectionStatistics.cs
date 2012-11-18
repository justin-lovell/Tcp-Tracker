using System.Threading;

namespace TcpTracker
{
    public sealed class TcpConnectionStatistics
    {
        private int _connectedClientSockets;
        private int _connectedRelaySockets;

        public int ConnectedClientSockets
        {
            get { return this._connectedClientSockets; }
        }

        public int ConnectedRelaySockets
        {
            get { return this._connectedRelaySockets; }
        }

        public void IncrementConnectedSockets()
        {
            Interlocked.Increment(ref this._connectedClientSockets);
            Interlocked.Increment(ref this._connectedRelaySockets);
        }

        public void DecrementConnectedSocket(TcpRelayDirection direction)
        {
            if (direction == TcpRelayDirection.RelayToClient)
            {
                Interlocked.Decrement(ref this._connectedRelaySockets);
            }
            else
            {
                Interlocked.Decrement(ref this._connectedClientSockets);
            }
        }

        public void DataTransmitted(TcpRelayDirection direction, int bytesRead)
        {
        }
    }
}