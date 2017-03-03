using System;
using System.Collections.Concurrent;
using System.Threading;
using Metrics;
using NServiceBus.Routing;
using NServiceBus.Transport;

namespace NServiceBus.Monitoring
{
    class Marker
    {
        public Marker(MetricsContext metrics, string endpointName, string uniqueInstanceId, Func<string, string> canonicalAddressGenerator)
        {
            this.metrics = metrics;
            this.endpointName = endpointName;
            this.uniqueInstanceId = uniqueInstanceId;
            this.canonicalAddressGenerator = canonicalAddressGenerator;
            this.tags = new MetricTags("type:sent", "endpoint:" + this.endpointName);
        }

        public void Mark(TransportOperation operation)
        {
            var headers = operation.Message.Headers;

            var unicastTag = operation.AddressTag as UnicastAddressTag;
            if (unicastTag != null)
            {
                var canonicalDestination = canonicalAddressGenerator(unicastTag.Destination);

                var key = $"{endpointName}.{uniqueInstanceId}.{canonicalDestination}";

                headers[$"NServiceBus.QueueLength.{canonicalDestination}.SequenceValue"] = GenerateNextValue(key).ToString();
                headers[$"NServiceBus.QueueLength.{canonicalDestination}.Key"] = key;
            }
            else
            {
                var multicastTag = operation.AddressTag as MulticastAddressTag;
                if (multicastTag != null)
                {
                    var key = $"{endpointName}.{uniqueInstanceId}.{multicastTag.MessageType.FullName}";

                    headers["NServiceBus.QueueLength.SequenceValue"] = GenerateNextValue(key).ToString();
                    headers["NServiceBus.QueueLength.Key"] = key;
                }
            }
        }

        long GenerateNextValue(string sequenceKey)
        {
            var f = linkState.GetOrAdd(sequenceKey, s =>
            {
                metrics.Gauge(sequenceKey, () => GetValue(sequenceKey), unit, tags);
                return new Sequence();
            });
            return f.GenerateNextValue();
        }

        double GetValue(string sequenceKey)
        {
            Sequence s;
            return linkState.TryGetValue(sequenceKey, out s) ? s.LastGeneratedMarker : 0;
        }

        MetricsContext metrics;
        string endpointName;
        string uniqueInstanceId;
        Func<string, string> canonicalAddressGenerator;
        ConcurrentDictionary<string, Sequence> linkState = new ConcurrentDictionary<string, Sequence>();
        MetricTags tags;
        Unit unit = Unit.Custom("Messages");

        class Sequence
        {
            public long LastGeneratedMarker;

            public long GenerateNextValue()
            {
                return Interlocked.Increment(ref LastGeneratedMarker);
            }
        }
    }
}