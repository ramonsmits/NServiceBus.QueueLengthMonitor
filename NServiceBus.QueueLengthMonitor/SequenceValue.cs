using System;

namespace NServiceBus.QueueLengthMonitor
{
    struct SequenceValue
    {
        public readonly long Value;
        public readonly DateTime Timestamp;

        public SequenceValue(long value, DateTime timestamp)
        {
            Value = value;
            Timestamp = timestamp;
        }
    }
}