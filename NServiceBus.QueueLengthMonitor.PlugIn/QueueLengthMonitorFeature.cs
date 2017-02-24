using System;
using System.Threading.Tasks;
using Metrics;
using NServiceBus.Features;
using NServiceBus.Hosting;
using NServiceBus.Settings;
using NServiceBus.Transport;

namespace NServiceBus.QueueLengthMonitor.PlugIn
{
    class QueueLengthMonitorFeature : Feature
    {
        protected override void Setup(FeatureConfigurationContext context)
        {
            var monitorQueue = context.Settings.Get<string>("NServiceBus.QueueLengthMonitor.MonitorQueue");
            var transportInfra = context.Settings.Get<TransportInfrastructure>();

            context.Container.ConfigureComponent<MetricsContext>(b => new DefaultMetricsContext(GetContextName(context.Settings, b.Build<HostInformation>())), DependencyLifecycle.SingleInstance);
            context.Container.ConfigureComponent(b => new MetricsConfig(b.Build<MetricsContext>()), DependencyLifecycle.SingleInstance);

            var uniqueId = context.Settings.LogicalAddress().EndpointInstance.Discriminator ?? Guid.NewGuid().ToString("N");

            context.Container.ConfigureComponent<Marker>(b => new Marker(b.Build<MetricsContext>(), context.Settings.EndpointName(), uniqueId, transportInfra.MakeCanonicalForm), DependencyLifecycle.SingleInstance);

            context.Pipeline.Register(b => new MarkerBehavior(b.Build<Marker>()), "Stamps outgoing messages with link state markers");
            context.Pipeline.Register(b => new MarkerProcessorBehavior(b.Build<MetricsContext>(), context.Settings.LocalAddress(), context.Settings.EndpointName()), "Processes incoming link state markers");

            context.RegisterStartupTask(b => new ReporterStarter(b.Build<IDispatchMessages>(), monitorQueue, b.Build<MetricsConfig>()));
        }

        static string GetContextName(ReadOnlySettings settings, HostInformation hostInfo)
        {
            return $"{settings.EndpointName()}-{hostInfo.HostId}";
        }

        class ReporterStarter : FeatureStartupTask
        {
            IDispatchMessages dispatcher;
            string monitorQueue;
            MetricsConfig metricsConfig;

            public ReporterStarter(IDispatchMessages dispatcher, string monitorQueue, MetricsConfig metricsConfig)
            {
                this.dispatcher = dispatcher;
                this.monitorQueue = monitorQueue;
                this.metricsConfig = metricsConfig;
            }

            protected override Task OnStart(IMessageSession session)
            {
                metricsConfig.WithReporting(r =>
                {
                    r.WithReport(new NServiceBusMetricReporter(dispatcher, monitorQueue), TimeSpan.FromSeconds(5));
                });
                return Task.CompletedTask;
            }

            protected override Task OnStop(IMessageSession session)
            {
                return Task.CompletedTask;
            }
        }
    }
}