using System;
using System.Collections.Concurrent;
using System.Threading;
using Metrics.MetricData;
using Metrics.Reporters;

namespace Metrics.NET.PerformanceCounters
{
    public class PerformaceCounterReporter : MetricsReport
    {
        Func<MetricInfo, CounterInstanceName> counterInstanceNameProvider;

        public PerformaceCounterReporter(Func<MetricInfo, CounterInstanceName> counterInstanceNameProvider)
        {
            this.counterInstanceNameProvider = counterInstanceNameProvider;
        }

        public void RunReport(MetricsData data, Func<HealthStatus> healthStatus, CancellationToken token)
        {
            Report(data);
        }

        void Report(MetricsData data, string prefix = "")
        {
            var prefixedContextName = prefix + data.Context;

            foreach (var gauge in data.Gauges)
            {
                var metricInfo = new MetricInfo(prefixedContextName, gauge.Name, gauge.Tags);
                var counterIntanceName = counterInstanceNameProvider(metricInfo);
                var counterInstance = GetCachedInstance(counterIntanceName);

                counterInstance.SetValue((long) gauge.Value);
            }

            foreach (var childData in data.ChildMetrics)
            {
                Report(childData, prefixedContextName + ".");
            }
        }

        IPerformanceCounterInstance GetCachedInstance(CounterInstanceName name)
        {
            return counterCache.GetOrAdd(name,
                x => PerformanceCounterHelper.TryToInstantiatePerformanceCounter(name.CounterName, name.InstanceName));
        }

        ConcurrentDictionary<CounterInstanceName, IPerformanceCounterInstance> counterCache = new ConcurrentDictionary<CounterInstanceName, IPerformanceCounterInstance>();
    }
}
