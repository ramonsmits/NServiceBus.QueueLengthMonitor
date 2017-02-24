namespace Metrics.NET.PerformanceCounters
{
    class NonFunctionalPerformanceCounterInstance : IPerformanceCounterInstance
    {
        public void Dispose()
        {
            //NOOP
        }

        public void SetValue(long value)
        {
            //NOOP
        }
    }
}