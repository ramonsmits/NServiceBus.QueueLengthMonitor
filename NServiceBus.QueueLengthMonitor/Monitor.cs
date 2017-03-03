using System.Text;
using System.Threading.Tasks;
using Metrics;
using Metrics.Json;
using Newtonsoft.Json;
using NServiceBus.Transport;

namespace ServiceControl.Monitoring
{
    public class Monitor
    {
        IDerivedMetric[] derivedMetrics;
        NServiceBusReceivedMetricContext receivedMetricContext;

        public Monitor(MetricsContext rootContext, params IDerivedMetric[] derivedMetrics)
        {
            this.derivedMetrics = derivedMetrics;
            receivedMetricContext = new NServiceBusReceivedMetricContext();
            rootContext.Advanced.AttachContext("NServiceBus.Endpoints", receivedMetricContext);

            foreach (var metric in derivedMetrics)
            {
                metric.Initialize(rootContext, receivedMetricContext);
            }
        }

        public Task OnMessage(MessageContext context, IDispatchMessages dispatcher)
        {
            var metricsData = Deserialize<JsonMetricsContext>(context);

            receivedMetricContext.Consume(metricsData.ToMetricsData());

            foreach (var metric in derivedMetrics)
            {
                metric.Consume(metricsData);
            }
            return Task.CompletedTask;
        }
        
        static T Deserialize<T>(MessageContext context)
        {
            return JsonConvert.DeserializeObject<T>(Encoding.UTF8.GetString(context.Body));
        }
    }
}