using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;

namespace TcpTracker
{
    public sealed class SummaryConsoleTcpDataLogger : ITcpDataLogger
    {
        private readonly Subject<TransmissionSummary> _transmissionSubject = new Subject<TransmissionSummary>();
        private readonly IDisposable _consoleSubscription;
        private int _connectedClients;

        public SummaryConsoleTcpDataLogger()
        {
            Func<IEnumerable<TransmissionSummary>, TcpRelayDirection, IEnumerable<TransmissionSummary>>
                extractSummariesForDirection
                    = (summaries, direction) => summaries.Where(x => x.Direction == direction).ToList();

            Func<IEnumerable<TransmissionSummary>, int> extractDistinctActiveHubs =
                summaries => summaries.Select(x => x.Hub).Distinct().Count();

            Func<IEnumerable<TransmissionSummary>, int> extractBytesSent =
                summaries => summaries.Select(x => x.ByteCount).Sum();

            this._consoleSubscription =
                this._transmissionSubject
                    .Buffer(TimeSpan.FromSeconds(1))
                    .Select(list =>
                                {
                                    IEnumerable<TransmissionSummary> serverSent =
                                        extractSummariesForDirection(list, TcpRelayDirection.RelayToClient);
                                    IEnumerable<TransmissionSummary> clientSent =
                                        extractSummariesForDirection(list, TcpRelayDirection.ClientToRelay);

                                    int uniqueuActiveHubs = extractDistinctActiveHubs(list);
                                    int distinctActiveServerHubs = extractDistinctActiveHubs(serverSent);
                                    int distinctActiveClientHubs = extractDistinctActiveHubs(clientSent);

                                    int totalBytesSent = extractBytesSent(list);
                                    int serverBytesSent = extractBytesSent(serverSent);
                                    int clientBytesSent = extractBytesSent(clientSent);

                                    return new
                                               {
                                                   Total = new
                                                               {
                                                                   ActiveHubs = uniqueuActiveHubs,
                                                                   BytesSent = totalBytesSent,
                                                               },
                                                   Server = new
                                                                {
                                                                    ActiveHubs = distinctActiveServerHubs,
                                                                    BytesSent = serverBytesSent,
                                                                },
                                                   Client = new
                                                                {
                                                                    ActiveHubs = distinctActiveClientHubs,
                                                                    BytesSent = clientBytesSent,
                                                                },
                                               };
                                })
                    .Subscribe(obj =>
                                   {
                                       Func<dynamic, string> createSummary =
                                           o => string.Format("{0}KB/s ({1} hubs)", o.BytesSent, o.ActiveHubs);

                                       Console.WriteLine(
                                           "{0:HH:mm:ss} - Total Hubs: {1} - Overall: {2} - Server: {3} - Client: {4}",
                                           DateTime.Now,
                                           this._connectedClients,
                                           createSummary(obj.Total),
                                           createSummary(obj.Server),
                                           createSummary(obj.Client)
                                           );
                                   });
        }

        public void ClientConnected(TcpExchangeHub hub)
        {
            Interlocked.Increment(ref this._connectedClients);
        }

        public void Disconnected(TcpExchangeHub hub, TcpRelayDirection direction)
        {
            if (direction == TcpRelayDirection.ClientToRelay)
            {
                Interlocked.Decrement(ref this._connectedClients);
            }
        }

        public void TransmitData(TcpExchangeHub hub, TcpRelayDirection direction, byte[] data, int count)
        {
            if (!this._transmissionSubject.HasObservers)
            {
                return;
            }

            var subject = new TransmissionSummary
                              {
                                  Hub = hub,
                                  ByteCount = count,
                                  Direction = direction,
                              };

            this._transmissionSubject.OnNext(subject);
        }

        public void ShuttingDown()
        {
            this._consoleSubscription.Dispose();
        }

        private struct TransmissionSummary
        {
            public TcpExchangeHub Hub;
            public int ByteCount;
            public TcpRelayDirection Direction;
        }
    }
}