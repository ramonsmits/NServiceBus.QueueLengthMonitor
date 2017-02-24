using System;

namespace Metrics.NET.PerformanceCounters
{
    interface IPerformanceCounterInstance : IDisposable
    {
        void SetValue(long value);
    }
}