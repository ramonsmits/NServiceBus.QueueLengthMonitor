using System.Collections.Generic;

namespace NServiceBus.QueueLengthMonitor
{
    class SequenceStateSnapshot
    {
        public string Key { get; }

        public ICollection<SequenceValue> SourceValues { get; }

        public SequenceValue DestinationValue { get; }

        public SequenceStateSnapshot(string key, ICollection<SequenceValue> sourceValues, SequenceValue destinationValue)
        {
            Key = key;
            SourceValues = sourceValues;
            DestinationValue = destinationValue;
        }
    }
}