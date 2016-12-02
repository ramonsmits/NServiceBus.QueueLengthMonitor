using System;

namespace NServiceBus.QueueLengthMonitor
{
    class SendingStateSequenceValue
    {
        public DateTime Timestamp { get; }
        public long Value { get; }

        public SendingStateSequenceValue(DateTime timestamp, long value)
        {
            Timestamp = timestamp;
            Value = value;
        }
    }
}