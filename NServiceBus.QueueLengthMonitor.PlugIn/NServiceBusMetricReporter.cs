using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Metrics;
using Metrics.Json;
using Metrics.MetricData;
using Metrics.Reporters;
using NServiceBus.Extensibility;
using NServiceBus.Routing;
using NServiceBus.Transport;

namespace NServiceBus.QueueLengthMonitor.PlugIn
{
    public class NServiceBusMetricReporter : MetricsReport
    {
        string destination;
        IDispatchMessages dispatcher;

        public NServiceBusMetricReporter(IDispatchMessages dispatcher, string destination)
        {
            this.dispatcher = dispatcher;
            this.destination = destination;
        }

        public void RunReport(MetricsData metricsData, Func<HealthStatus> healthStatus, CancellationToken token)
        {
            var serialized = JsonBuilderV2.BuildJson(metricsData);
            var body = Encoding.UTF8.GetBytes(serialized);

            var headers = new Dictionary<string, string>();
            var message = new OutgoingMessage(Guid.NewGuid().ToString(), headers, body);
            var operation = new TransportOperation(message, new UnicastAddressTag(destination));
            var task = dispatcher.Dispatch(new TransportOperations(operation), new TransportTransaction(), new ContextBag());
            task.GetAwaiter().GetResult();
        }
    }
}