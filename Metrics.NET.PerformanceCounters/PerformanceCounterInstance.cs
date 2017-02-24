using System.Diagnostics;

namespace Metrics.NET.PerformanceCounters
{
    class PerformanceCounterInstance : IPerformanceCounterInstance
    {
        public PerformanceCounterInstance(PerformanceCounter counter)
        {
            this.counter = counter;
        }
        
        public void Dispose()
        {
            counter.Dispose();
        }

        public void SetValue(long value)
        {
            counter.RawValue = value;
        }

        PerformanceCounter counter;
    }
}