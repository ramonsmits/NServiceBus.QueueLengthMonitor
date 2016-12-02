using System.Collections.Generic;

namespace NServiceBus.QueueLengthMonitor
{
    public class SourceReport
    {
        public List<Sequence> SequenceValues { get; set; } = new List<Sequence>();
    }
}