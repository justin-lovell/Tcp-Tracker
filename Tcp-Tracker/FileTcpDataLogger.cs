using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading;

namespace TcpTracker
{
    public class FileTcpDataLogger : ITcpDataLogger
    {
        private readonly object _lockObject = new object();
        private readonly string _logFilePath;

        private StreamWriter _fileWriter;

        public FileTcpDataLogger(string logFilePath)
        {
            this._logFilePath = logFilePath;
        }

        private void EnsureIsInitialized()
        {
            if (this._fileWriter != null)
            {
                return;
            }

            lock (this._lockObject)
            {
                if (this._fileWriter != null)
                {
                    return;
                }

                if (!File.Exists(this._logFilePath))
                {
                    string directory = Path.GetDirectoryName(this._logFilePath);
                    Debug.Assert(directory != null, "Directory was null");

                    if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }
                }

                var fs = new FileStream(this._logFilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite,
                                        FileShare.ReadWrite);

                fs.Position = fs.Length;

                this._fileWriter = new StreamWriter(fs);

                var flushingThread = new Thread(() =>
                                                    {
                                                        while (true)
                                                        {
                                                            Thread.Sleep(TimeSpan.FromSeconds(5));
                                                            this._fileWriter.Flush();
                                                        }
                                                    });

                flushingThread.Start();
            }
        }

        private void WriteLine(TcpExchangeHub hub, TcpRelayDirection direction, string message)
        {
            this.EnsureIsInitialized();

            DateTime dt = DateTime.Now;

            lock (this._lockObject)
            {
                this._fileWriter.WriteLine("{0:yyyy-MM-dd HH:mm:ss} {1} {2}+ {3}",
                                           dt,
                                           TcpDataLoggerHelper.GetShortInstanceName(hub),
                                           TcpDataLoggerHelper.GetDirectionShortCode(direction),
                                           message
                    );
            }
        }

        public void ClientConnected(TcpExchangeHub hub)
        {
            string information = string.Format("Client Connected from {0}", hub.ClientEndPoint);
            this.WriteLine(hub, TcpRelayDirection.ClientToRelay, information);
        }

        public void Disconnected(TcpExchangeHub hub, TcpRelayDirection direction)
        {
            this.WriteLine(hub, direction, "Disconnected");
        }

        public void TransmitData(TcpExchangeHub hub, TcpRelayDirection direction, byte[] data, int count)
        {
            var hex = new string[data.Length];
            var charRep = new string[count];

            for (int i = 0; i < data.Length; i++)
            {
                hex[i] = " ";
            }

            for (int i = 0; i < count; i++)
            {
                var c = (char)data[i];

                hex[i] = data[i].ToString("X2");
                charRep[i] = (char.IsControl(c) || char.IsWhiteSpace(c))
                                 ? "."
                                 : c.ToString(CultureInfo.InvariantCulture);
            }

            string info = string.Format("{0}   {1}",
                                      string.Join(" ", hex),
                                      string.Join(" ", charRep)
                );
            this.WriteLine(hub, direction, info);
        }
    }
}