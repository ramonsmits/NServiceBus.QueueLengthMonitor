using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace NServiceBus.QueueLengthMonitor
{
    class SourceReports
    {
        public void Add(SourceReport report, DateTime timestamp)
        {
            foreach (var value in report.SequenceValues)
            {
                sequences.AddOrUpdate(value.Key,
                    key =>
                {
                    var state = new SequenceState();
                    state.Update(value.Value, timestamp);
                    return state;
                }, (key, current) =>
                {
                    current.Update(value.Value, timestamp);
                    return current;
                });
            }
        }

        public IEnumerable<SequenceStateSnapshot> GetSequenceState(DestinationReport report, DateTime timestamp)
        {
            return report.SequenceValues.Select(d => new { d, s = TryGetSequence(d.Key) })
                .Where(x => x.s != null)
                .Select(x => new SequenceStateSnapshot(x.d.Key, x.s.GetValues(), new SequenceValue(x.d.Value, timestamp)));
        }

        SequenceState TryGetSequence(string key)
        {
            SequenceState state;
            sequences.TryGetValue(key, out state);
            return state;
        }

        ConcurrentDictionary<string, SequenceState> sequences = new ConcurrentDictionary<string, SequenceState>();

        class SequenceState
        {
            const int BufferSize = 5;

            int index;
            SequenceValue[] values = new SequenceValue[BufferSize];

            public void Update(long newValue, DateTime newTimestamp)
            {
                var i = Interlocked.Increment(ref index);
                values[i % BufferSize] = new SequenceValue(newValue, newTimestamp);
            }

            public ICollection<SequenceValue> GetValues()
            {
                return new List<SequenceValue>(values);
            }
        }
    }
}
