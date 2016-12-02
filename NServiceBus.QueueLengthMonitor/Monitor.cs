using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Metrics;
using Newtonsoft.Json;
using NServiceBus.Transport;

namespace NServiceBus.QueueLengthMonitor
{
    public class Monitor
    {
        SourceReports sourceReports = new SourceReports();
        ConcurrentDictionary<string, long> queueLengths = new ConcurrentDictionary<string, long>();

        public Monitor()
        {
            Metric.Config
                .WithHttpEndpoint("http://localhost:7777/QueueLengthMonitor/");
        }

        public Task OnMessage(MessageContext context, IDispatchMessages dispatcher)
        {
            string type;
            if (!context.Headers.TryGetValue("NServiceBus.QueueLength.Type", out type))
            {
                return Task.CompletedTask;
            }
            if (type == "source-report")
            {
                var report = Deserialize<SourceReport>(context);
                sourceReports.Add(report, DateTime.UtcNow);
            }
            else if (type == "destination-report")
            {
                var report = Deserialize<DestinationReport>(context);

                var stateSnapshot = sourceReports.GetSequenceState(report, DateTime.UtcNow);

                var length = Calculate(stateSnapshot);

                queueLengths.AddOrUpdate(report.Queue, x =>
                {
                    Metric.Gauge(report.Queue, () => queueLengths[report.Queue], Unit.Custom("Messages"));
                    return length;
                }, (key, v) => length);
            }
            return Task.CompletedTask;
        }

        static long Calculate(IEnumerable<SequenceStateSnapshot> state)
        {
            return state.Sum(GetNumberOfInFlightMessages);
        }

        static int GetNumberOfInFlightMessages(SequenceStateSnapshot sequenceStateSnapshot)
        {
            var mostRecentDataPoints = sequenceStateSnapshot.SourceValues.OrderByDescending(x => x.Value).Take(2).ToArray();
            if (mostRecentDataPoints.Length == 0)
            {
                return 0;
            }

            if (mostRecentDataPoints.Length == 1)
            {
                return (int) (mostRecentDataPoints[0].Value - sequenceStateSnapshot.DestinationValue.Value);
            }

            var difference = mostRecentDataPoints[0].Value - mostRecentDataPoints[1].Value;
            var time = (mostRecentDataPoints[0].Timestamp - mostRecentDataPoints[1].Timestamp).TotalSeconds;

            var throughput = difference/time;

            var delay = (sequenceStateSnapshot.DestinationValue.Timestamp - mostRecentDataPoints[0].Timestamp).TotalSeconds;

            var estimatedNumberOfGeneratedMessages = (int) (delay*throughput);

            return (int)mostRecentDataPoints[0].Value + estimatedNumberOfGeneratedMessages - (int)sequenceStateSnapshot.DestinationValue.Value;
        }

        static T Deserialize<T>(MessageContext context)
        {
            return JsonConvert.DeserializeObject<T>(Encoding.UTF8.GetString(context.Body));
        }
    }
}