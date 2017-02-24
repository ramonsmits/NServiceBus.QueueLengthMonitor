namespace Metrics.NET.PerformanceCounters
{
    public class MetricInfo
    {
        public readonly string ContextName;
        public readonly string MetricName;
        public readonly MetricTags MetricTags;

        public MetricInfo(string contextName, string metricName, MetricTags metricTags)
        {
            ContextName = contextName;
            MetricName = metricName;
            MetricTags = metricTags;
        }
    }
}