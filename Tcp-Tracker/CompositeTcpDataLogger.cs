using System.Collections.Generic;

namespace TcpTracker
{
    public sealed class CompositeTcpDataLogger : ITcpDataLogger
    {
        private readonly List<ITcpDataLogger> _loggers = new List<ITcpDataLogger>();

        public void AppendLogger(ITcpDataLogger logger)
        {
            this._loggers.Add(logger);
        }

        public void ClientConnected(TcpExchangeHub hub)
        {
            foreach (var logger in this._loggers)
            {
                logger.ClientConnected(hub);
            }
        }

        public void Disconnected(TcpExchangeHub hub, TcpRelayDirection direction)
        {
            foreach (var logger in this._loggers)
            {
                logger.Disconnected(hub, direction);
            }
        }

        public void TransmitData(TcpExchangeHub hub, TcpRelayDirection direction, byte[] data, int count)
        {
            foreach (var logger in this._loggers)
            {
                logger.TransmitData(hub, direction, data, count);
            }
        }

        public void ShuttingDown()
        {
            foreach (var logger in this._loggers)
            {
                logger.ShuttingDown();
            }
        }
    }
}