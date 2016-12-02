using System.Linq;
using NServiceBus.Transport;

namespace NServiceBus.QueueLengthMonitor.PlugIn
{
    class DestinationReporter : Reporter
    {
        public DestinationReporter(MarkerProcessorBehavior behavior, string localAddress, IAsyncTimer timer, IDispatchMessages dispatcher, string monitorAddress) 
            : base(timer, dispatcher, monitorAddress, "destination-report")
        {
            this.behavior = behavior;
            this.localAddress = localAddress;
        }

        protected override object GenerateReport()
        {
            var report = new DestinationReport
            {
                Queue = localAddress,
                SequenceValues = behavior.Report().Select(x => new Sequence
                {
                    Key = x.Item1,
                    Value = x.Item2
                }).ToList()
            };
            return report;
        }

        MarkerProcessorBehavior behavior;
        string localAddress;
    }
}