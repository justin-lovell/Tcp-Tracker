namespace TcpTracker
{
    public static class TcpDataLoggerHelper
    {
        public static string GetShortInstanceName(object target)
        {
            return target.GetHashCode().ToString("X8");
        }

        public static string GetDirectionShortCode(TcpRelayDirection direction)
        {
            return direction == TcpRelayDirection.ClientToRelay ? "C" : "R";
        }
    }
}