using System.Linq;
using NServiceBus.Transport;

namespace NServiceBus.QueueLengthMonitor.PlugIn
{
    class SourceReporter : Reporter
    {
        public SourceReporter(MarkerBehavior behavior, IAsyncTimer timer, IDispatchMessages dispatcher, string monitorAddress) 
            : base(timer, dispatcher, monitorAddress, "source-report")
        {
            this.behavior = behavior;
        }

        protected override object GenerateReport()
        {
            var report = new SourceReport
            {
                SequenceValues = behavior.Report().Select(x => new Sequence
                {
                    Key = x.Item1,
                    Value = x.Item2
                }).ToList()
            };
            return report;
        }

        MarkerBehavior behavior;
    }
}