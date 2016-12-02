using System.Collections.Generic;

namespace NServiceBus.QueueLengthMonitor
{
    public class DestinationReport
    {
        public string Queue { get; set; }
        public List<Sequence> SequenceValues { get; set; } = new List<Sequence>();
    }
}