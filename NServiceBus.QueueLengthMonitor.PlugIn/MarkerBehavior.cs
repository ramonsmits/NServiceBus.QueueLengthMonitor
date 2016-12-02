using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NServiceBus.Pipeline;
using NServiceBus.Routing;

namespace NServiceBus.QueueLengthMonitor.PlugIn
{
    class MarkerBehavior : Behavior<IDispatchContext>
    {
        public MarkerBehavior(string endpointName, string uniqueInstanceId)
        {
            this.endpointName = endpointName;
            this.uniqueInstanceId = uniqueInstanceId;
        }

        public override Task Invoke(IDispatchContext context, Func<Task> next)
        {
            foreach (var operation in context.Operations)
            {
                var headers = operation.Message.Headers;

                var unicastTag = operation.AddressTag as UnicastAddressTag;
                if (unicastTag != null)
                {
                    var key = $"{endpointName}.{uniqueInstanceId}.{unicastTag.Destination}";

                    headers["NServiceBus.QueueLength.SequenceValue"] = GenerateNextValue(key).ToString();
                    headers["NServiceBus.QueueLength.Key"] = key;

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
            return next();
        }

        public IEnumerable<Tuple<string, long>> Report()
        {
            return data.ToArray().Select(kvp => Tuple.Create(kvp.Key, kvp.Value.LastGeneratedMarker));
        }

        long GenerateNextValue(string sequenceKey)
        {
            var f = data.GetOrAdd(sequenceKey, s => new Sequence());
            return f.GenerateNextValue();
        }

        string endpointName;
        string uniqueInstanceId;
        ConcurrentDictionary<string, Sequence> data = new ConcurrentDictionary<string, Sequence>();

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
