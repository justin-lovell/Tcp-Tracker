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
        private int _connectedClients;

        public SummaryConsoleTcpDataLogger()
        {
            Func<IEnumerable<TransmissionSummary>, TcpRelayDirection, IEnumerable<TransmissionSummary>>
                extractSummariesForDirection
                    = (summaries, direction) => summaries.Where(x => x.Direction == direction).ToList();

            Func<IEnumerable<TransmissionSummary>, decimal> extractDistinctActiveHubs =
                summaries => summaries.Select(x => x.Hub).Distinct().Count();

            Func<IEnumerable<TransmissionSummary>, decimal> extractAverageBytesSent =
                summaries => summaries.Select(x => x.ByteCount).Sum();

            this._transmissionSubject
                .Buffer(TimeSpan.FromSeconds(1))
                .Select(list =>
                            {
                                IEnumerable<TransmissionSummary> serverSent =
                                    extractSummariesForDirection(list, TcpRelayDirection.RelayToClient);
                                IEnumerable<TransmissionSummary> clientSent =
                                    extractSummariesForDirection(list, TcpRelayDirection.ClientToRelay);

                                decimal uniqueuActiveHubs = extractDistinctActiveHubs(list);
                                decimal distinctActiveServerHubs = extractDistinctActiveHubs(serverSent);
                                decimal distinctActiveClientHubs = extractDistinctActiveHubs(clientSent);

                                decimal totalAverageBytesSent = extractAverageBytesSent(list);
                                decimal serverAverageBytesSent = extractAverageBytesSent(serverSent);
                                decimal clientAverageBytesSent = extractAverageBytesSent(clientSent);

                                return new
                                           {
                                               Total = new
                                                           {
                                                               ActiveHubs = uniqueuActiveHubs,
                                                               AverageBytesSent = totalAverageBytesSent,
                                                           },
                                               Server = new
                                                            {
                                                                ActiveHubs = distinctActiveServerHubs,
                                                                AverageBytesSent = serverAverageBytesSent,
                                                            },
                                               Client = new
                                                            {
                                                                ActiveHubs = distinctActiveClientHubs,
                                                                AverageBytesSent = clientAverageBytesSent,
                                                            },
                                           };
                            })
                .Subscribe(obj => Console.WriteLine("{0:HH:mm:ss} - {1}", DateTime.Now, obj));
        }

        public void ClientConnected(TcpExchangeHub hub)
        {
            Interlocked.Increment(ref this._connectedClients);
        }

        public void Disconnected(TcpExchangeHub hub, TcpRelayDirection direction)
        {
            Interlocked.Decrement(ref this._connectedClients);
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

        private struct TransmissionSummary
        {
            public TcpExchangeHub Hub;
            public int ByteCount;
            public TcpRelayDirection Direction;
        }
    }
}