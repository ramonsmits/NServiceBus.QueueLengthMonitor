using NServiceBus.Configuration.AdvanceExtensibility;
using NServiceBus.Features;

namespace NServiceBus.Monitoring
{
    public static class QueueLengthMonitorFeatureExtensions
    {
        public static void EnableQueueLengthMonitorPlugin(this EndpointConfiguration config, string monitorQueue)
        {
            config.GetSettings().Set("NServiceBus.QueueLengthMonitor.MonitorQueue", monitorQueue);
            config.GetSettings().EnableFeatureByDefault<QueueLengthMonitorFeature>();
        }
    }
}