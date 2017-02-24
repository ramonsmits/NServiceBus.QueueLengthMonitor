using System;

namespace Metrics.NET.PerformanceCounters
{
    public class CounterInstanceName
    {
        public readonly string CounterName;
        public readonly string InstanceName;

        public CounterInstanceName(string counterName, string instanceName)
        {
            if (counterName == null)
            {
                throw new ArgumentNullException(nameof(counterName));
            }
            if (instanceName == null)
            {
                throw new ArgumentNullException(nameof(instanceName));
            }
            CounterName = counterName;
            InstanceName = instanceName;
        }

        protected bool Equals(CounterInstanceName other)
        {
            return string.Equals(CounterName, other.CounterName) && string.Equals(InstanceName, other.InstanceName);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((CounterInstanceName) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (CounterName.GetHashCode()*397) ^ InstanceName.GetHashCode();
            }
        }
    }
}