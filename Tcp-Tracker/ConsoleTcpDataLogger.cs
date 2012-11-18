using System;

namespace TcpTracker
{
    public sealed class ConsoleTcpDataLogger : ITcpDataLogger
    {
        public void ClientConnected(TcpExchangeHub hub)
        {
            string information = string.Format("Client Connected from {0}", hub.ClientEndPoint);
            WriteLine(hub, TcpRelayDirection.ClientToRelay, information);
        }

        public void Disconnected(TcpExchangeHub hub, TcpRelayDirection direction)
        {
            WriteLine(hub, direction, "Disconnected");
        }

        public void TransmitData(TcpExchangeHub hub, TcpRelayDirection direction, byte[] data, int count)
        {
            var hex = new string[count];

            for (int i = 0; i < count; i++)
            {
                hex[i] = data[i].ToString("X2");
            }

            string info = string.Join(" ", hex);
            WriteLine(hub, direction, info);
        }

        public void ShuttingDown()
        {
        }

        private static void WriteLine(TcpExchangeHub hub, TcpRelayDirection direction, string information)
        {
            Console.WriteLine("{0:HH:mm:ss.ff} {1} {2} {3}",
                              DateTime.Now,
                              TcpDataLoggerHelper.GetDirectionShortCode(direction),
                              TcpDataLoggerHelper.GetShortInstanceName(hub),
                              information);
        }
    }
}