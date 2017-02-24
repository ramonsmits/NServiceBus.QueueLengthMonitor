using System.Collections.Generic;
using System.Diagnostics;

namespace Demo
{
    public class PerformanceCounters
    {
        const string categoryName = "NServiceBus.Monitoring";

        public static void EnsureCreated()
        {
            if (DoesCategoryExist())
            {
                DeleteCategory();
            }
            SetupCounters();
        }

        static bool CheckCountersExist()
        {
            foreach (var counter in Counters)
            {
                if (!PerformanceCounterCategory.CounterExists(counter.CounterName, categoryName))
                    return false;
            }
            return true;
        }

        static bool DoesCategoryExist()
        {
            return PerformanceCounterCategory.Exists(categoryName);
        }

        static void DeleteCategory()
        {
            PerformanceCounterCategory.Delete(categoryName);
        }

        static void SetupCounters()
        {
            var counterCreationCollection = new CounterCreationDataCollection(Counters.ToArray());
            PerformanceCounterCategory.Create(categoryName, "NServiceBus statistics", PerformanceCounterCategoryType.MultiInstance, counterCreationCollection);
            PerformanceCounter.CloseSharedResources(); // http://blog.dezfowler.com/2007/08/net-performance-counter-problems.html
        }

        static List<CounterCreationData> Counters = new List<CounterCreationData>
                    {
                        new CounterCreationData("Queue Length", "Length of the queue.", PerformanceCounterType.NumberOfItems64),
                    };
    }
}