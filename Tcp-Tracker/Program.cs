using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using NDesk.Options;

namespace TcpTracker
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            EnvironmentOptions options = BuildEnvironmentOptions(args);

            if (options == null)
            {
                return;
            }

            IPAddress ipAddress = IPAddress.Any;
            var listeningSocket = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.IP);

            var statistics = new TcpConnectionStatistics();

            listeningSocket.Bind(new IPEndPoint(ipAddress, options.ListenOnPort));
            listeningSocket.Listen(0);

            var logger = new CompositeTcpDataLogger();
            //logger.AppendLogger(new ConsoleTcpDataLogger());

            logger.AppendLogger(new SummaryConsoleTcpDataLogger());
            
            if (!string.IsNullOrEmpty(options.LogFile))
                logger.AppendLogger(new FileTcpDataLogger(options.LogFile));

            while (true)
            {
                Socket clientSocket = null;
                Socket relaySocket = null;

                Task<Socket>.Factory.FromAsync(listeningSocket.BeginAccept, listeningSocket.EndAccept, null)
                    .ContinueWith(task =>
                                      {
                                          clientSocket = task.Result;

                                          return Task<IPHostEntry>.Factory.FromAsync(Dns.BeginGetHostEntry,
                                                                                     Dns.EndGetHostEntry,
                                                                                     options.ForwardToHostAddress,
                                                                                     null);
                                      })
                    .Unwrap()
                    .ContinueWith(task =>
                                      {
                                          IPHostEntry relayHostEntry = task.Result;
                                          IPAddress relayHostIpAddress = relayHostEntry.AddressList.First();

                                          relaySocket = new Socket(relayHostIpAddress.AddressFamily,
                                                                   SocketType.Stream,
                                                                   ProtocolType.IP);

                                          return Task.Factory.FromAsync(relaySocket.BeginConnect,
                                                                        relaySocket.EndConnect,
                                                                        relayHostEntry.AddressList,
                                                                        options.ForwardToPort,
                                                                        null);
                                      })
                    .Unwrap()
                    .ContinueWith(task =>
                                      {
                                          statistics.IncrementConnectedSockets();

                                          var exchange = new TcpExchangeHub(statistics, logger);

                                          exchange.WireExchange(relaySocket, clientSocket);
                                      })
                    .Wait();
            }
        }

        private static EnvironmentOptions BuildEnvironmentOptions(IEnumerable<string> args)
        {
            int listenPort = 0;
            string forwardToHost = null;
            int forwardToPort = 0;
            string logFile = null;
            bool displayHelp = false;
            var optionSet = new OptionSet
                                {
                                    {"listenPort=", "Port to listen on", new Action<int>(i => listenPort = i)},
                                    {"forwardToHost=", "Host to forward the traffic to", s => forwardToHost = s},
                                    {
                                        "forwardToPort=",
                                        "Port number on host to forward to",
                                        new Action<int>(i => forwardToPort = i)
                                    },
                                    {"logFilePath:", s => logFile = s},
                                    {"displayHelp", new Action<bool>(b => displayHelp = b)},
                                };

            optionSet.Parse(args);

            if (displayHelp)
            {
                Console.WriteLine("Usage: Tcp-Tracker.exe [OPTIONS]");
                Console.WriteLine();
                Console.WriteLine("Options:");
                Console.WriteLine();

                optionSet.WriteOptionDescriptions(Console.Out);

                return null;
            }

            return new EnvironmentOptions(listenPort, forwardToHost, forwardToPort, logFile);
        }
    }
}