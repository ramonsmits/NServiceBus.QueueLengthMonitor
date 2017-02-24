using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Metrics;
using Metrics.Core;
using Metrics.Json;
using Metrics.MetricData;
using Newtonsoft.Json;
using NServiceBus.Transport;

namespace NServiceBus.QueueLengthMonitor
{
    public class Monitor
    {
        MetricsContext rootContext;
        NServiceBusReceivedMetricContext receivedMetricContext;
        MetricsContext receiveLinkState;
        MetricsContext sendLinkState;
        MetricsContext linkState;
        MetricsContext queueState;
        Unit messagesUnit = Unit.Custom("Messages");
        Unit sequenceUnit = Unit.Custom("Sequence");

        public Monitor(MetricsContext rootContext)
        {
            this.rootContext = rootContext;
            receivedMetricContext = new NServiceBusReceivedMetricContext();
            rootContext.Advanced.AttachContext("NServiceBus.Endpoints", receivedMetricContext);
            receiveLinkState = rootContext.Context("ReceivedLinkState");
            sendLinkState = rootContext.Context("SentLinkState");
            linkState = rootContext.Context("LinkState");
            queueState = rootContext.Context("QueueState");
        }

        public Task OnMessage(MessageContext context, IDispatchMessages dispatcher)
        {
            var metricsData = Deserialize<JsonMetricsContext>(context);

            receivedMetricContext.Consume(metricsData.ToMetricsData());

            var sendSideLinkStateByKey = metricsData.Gauges.Where(g => g.Tags.Contains("type:sent")).GroupBy(g => g.Name);
            foreach (var keyState in sendSideLinkStateByKey)
            {
                var sequenceKey = keyState.Key;
                sendLinkState.Gauge(sequenceKey, () => GetHighestSequenceNumber(sequenceKey, receivedMetricContext, "type:sent"), sequenceUnit);
            }

            var queues = new HashSet<string>();
            var receiveSideLinkStateByKey = metricsData.Gauges.Where(g => g.Tags.Contains("type:received")).GroupBy(g => g.Name);
            foreach (var keyState in receiveSideLinkStateByKey)
            {
                var sequenceKey = keyState.Key;
                var queue = QueueTag(keyState.First().Tags);
                queues.Add(queue);
                receiveLinkState.Gauge(sequenceKey, () => GetHighestSequenceNumber(sequenceKey, receivedMetricContext, "type:received"), sequenceUnit, new MetricTags($"queue:{queue}"));
                linkState.Gauge(sequenceKey, () => GetNumberOfInFlightMessages(sequenceKey, receiveLinkState, sendLinkState), messagesUnit, new MetricTags($"queue:{queue}"));
            }

            foreach (var queue in queues)
            {
                queueState.Gauge(queue, () => GetQueueState(queue, linkState), messagesUnit);
            }
            return Task.CompletedTask;
        }

        static double GetQueueState(string queue, MetricsContext linkStateContext)
        {
            var linkStateGauges =
                linkStateContext.DataProvider.CurrentMetricsData.Gauges.Where(g =>
                    String.Equals(QueueTag(g.Tags), queue, StringComparison.OrdinalIgnoreCase));

            var inFlight = linkStateGauges.Select(g => g.Value);
            return inFlight.Sum();
        }

        static double GetNumberOfInFlightMessages(string sequenceKey, MetricsContext receiveStateContext, MetricsContext sendStateContext)
        {
            var receiveGauge =
                receiveStateContext.DataProvider.CurrentMetricsData.Gauges.First(g => g.Name == sequenceKey);
            var sentGauge =
                sendStateContext.DataProvider.CurrentMetricsData.Gauges.FirstOrDefault(g => g.Name == sequenceKey);

            if (sentGauge == null)
            {
                return 0;
            }
            return sentGauge.Value - receiveGauge.Value;
        }

        static double GetHighestSequenceNumber(string sequenceKey, MetricsContext metricsContext, string type)
        {
            var data = metricsContext.DataProvider.CurrentMetricsData;

            var matchingType = data.Flatten()
                .Gauges.Where(g => g.Tags.Contains(type));

            var receiveSideLinkStateForThisQueue = matchingType
                .Where(g => g.Name == sequenceKey)
                .Select(g => g.Value);

            return receiveSideLinkStateForThisQueue.Max();
        }

        static string QueueTag(string[] tags)
        {
            var queueTag = tags.FirstOrDefault(t => t.StartsWith("queue:", StringComparison.OrdinalIgnoreCase));
            return queueTag?.Substring(6);
        }
        
        static T Deserialize<T>(MessageContext context)
        {
            return JsonConvert.DeserializeObject<T>(Encoding.UTF8.GetString(context.Body));
        }
    }
}