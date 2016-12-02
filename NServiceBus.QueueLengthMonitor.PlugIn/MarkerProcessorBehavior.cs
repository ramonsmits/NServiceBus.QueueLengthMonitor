using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NServiceBus.Pipeline;

namespace NServiceBus.QueueLengthMonitor.PlugIn
{
    class MarkerProcessorBehavior : Behavior<IIncomingPhysicalMessageContext>
    {
        public override Task Invoke(IIncomingPhysicalMessageContext context, Func<Task> next)
        {
            string sequence;
            if (context.MessageHeaders.TryGetValue("NServiceBus.QueueLength.SequenceValue", out sequence))
            {
                var sequenceValue = long.Parse(sequence);
                var key = context.MessageHeaders["NServiceBus.QueueLength.Key"];

                MarkerReceived(key, sequenceValue);
            }
            return next();
        }

        void MarkerReceived(string key, long sequenceValue)
        {
            linkState.AddOrUpdate(key, sequenceValue, (k, value) => sequenceValue > value ? sequenceValue : value);
        }

        public IEnumerable<Tuple<string, long>> Report()
        {
            return linkState.ToArray().Select(kvp => Tuple.Create(kvp.Key, kvp.Value));
        }

        ConcurrentDictionary<string, long> linkState = new ConcurrentDictionary<string, long>();
    }
}