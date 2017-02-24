using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Metrics;
using NServiceBus.Pipeline;

namespace NServiceBus.QueueLengthMonitor.PlugIn
{
    class MarkerProcessorBehavior : Behavior<IIncomingPhysicalMessageContext>
    {
        public MarkerProcessorBehavior(MetricsContext metrics, string localAddress, string endpoint)
        {
            this.tags = new MetricTags("type:received", "queue:"+localAddress, "endpoint:"+endpoint);
            this.metrics = metrics;
            this.localAddress = localAddress;
        }

        public override Task Invoke(IIncomingPhysicalMessageContext context, Func<Task> next)
        {
            string sequence;
            if (context.MessageHeaders.TryGetValue($"NServiceBus.QueueLength.{localAddress}.SequenceValue", out sequence))
            {
                var sequenceValue = long.Parse(sequence);
                var key = context.MessageHeaders[$"NServiceBus.QueueLength.{localAddress}.Key"];

                MarkerReceived(key, sequenceValue);
            }
            else
            {
                Console.Write("Dupa");
            }
            return next();
        }

        void MarkerReceived(string key, long sequenceValue)
        {
            linkState.AddOrUpdate(key, k =>
            {
                metrics.Gauge(key, () => GetValue(key), unit, tags);
                return sequenceValue;
            }, (k, value) => sequenceValue > value ? sequenceValue : value);
        }

        double GetValue(string key)
        {
            long value;
            linkState.TryGetValue(key, out value);
            return value;
        }
        
        ConcurrentDictionary<string, long> linkState = new ConcurrentDictionary<string, long>();
        MetricsContext metrics;
        string localAddress;
        MetricTags tags;
        Unit unit = Unit.Custom("Messages");
    }
}