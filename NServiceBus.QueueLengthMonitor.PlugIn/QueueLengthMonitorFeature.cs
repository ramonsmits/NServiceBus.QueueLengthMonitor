using System;
using NServiceBus.Features;
using NServiceBus.Transport;

namespace NServiceBus.QueueLengthMonitor.PlugIn
{
    class QueueLengthMonitorFeature : Feature
    {
        protected override void Setup(FeatureConfigurationContext context)
        {
            var monitorQueue = context.Settings.Get<string>("NServiceBus.QueueLengthMonitor.MonitorQueue");
            var uniqueId = context.Settings.LogicalAddress().EndpointInstance.Discriminator ?? Guid.NewGuid().ToString("N");

            var markerBahavior = new MarkerBehavior(context.Settings.EndpointName(), uniqueId);
            var markerProcessorBehavior = new MarkerProcessorBehavior();

            context.Pipeline.Register(markerBahavior, "Stamps outgoing messages with link state markers");
            context.Pipeline.Register(markerProcessorBehavior, "Processes incoming link state markers");

            context.RegisterStartupTask(b => new SourceReporter(markerBahavior, new AsyncTimer(), b.Build<IDispatchMessages>(), monitorQueue));
            context.RegisterStartupTask(b => new DestinationReporter(markerProcessorBehavior, context.Settings.LocalAddress(), new AsyncTimer(), b.Build<IDispatchMessages>(), monitorQueue));
        }
    }
}