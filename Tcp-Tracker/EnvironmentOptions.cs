namespace TcpTracker
{
    public class EnvironmentOptions
    {
        public EnvironmentOptions(int listenOnPort,
                                  string forwardToHostAddress,
                                  int forwardToPort,
                                  string logFile,
                                  bool detailedLoggingToConsole)
        {
            this.DetailedLoggingToConsole = detailedLoggingToConsole;
            this.ListenOnPort = listenOnPort;
            this.ForwardToHostAddress = forwardToHostAddress;
            this.ForwardToPort = forwardToPort;
            this.LogFile = logFile;
        }

        public int ListenOnPort { get; private set; }
        public string ForwardToHostAddress { get; private set; }
        public int ForwardToPort { get; private set; }
        public string LogFile { get; private set; }
        public bool DetailedLoggingToConsole { get; private set; }
    }
}