using System.Threading.Tasks;

namespace TcpTracker
{
    public interface ITcpDataLogger
    {
        void ClientConnected(TcpExchangeHub hub);

        void Disconnected(TcpExchangeHub hub, TcpRelayDirection direction);

        void TransmitData(TcpExchangeHub hub, TcpRelayDirection direction, byte[] data, int count);

        void ShuttingDown();
    }
}