using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UiPath.Monitoring.Server
{
    class GetPerformanceCounterList
    {
        public static List<PerformanceCounter> ListCounters(string[] categoryNames)
        {
            List<PerformanceCounter> lst_perfCounter = new List<PerformanceCounter>();
            foreach (string categoryName in categoryNames)
            {
                PerformanceCounter[] temp_pc = null;
                PerformanceCounterCategory category = PerformanceCounterCategory.GetCategories().First(c => c.CategoryName == categoryName);
                // Console.WriteLine("{0} [{1}]", category.CategoryName, category.CategoryType);

                string[] instanceNames = category.GetInstanceNames();

                if (instanceNames.Length > 0)
                {
                    // MultiInstance categories
                    foreach (string instanceName in instanceNames)
                    {
                        temp_pc = ListInstances(category, instanceName);
                    }
                }
                else
                {
                    // SingleInstance categories
                    temp_pc = ListInstances(category, string.Empty);
                }
                lst_perfCounter.AddRange(temp_pc);

            }
            return lst_perfCounter;
        }

        private static PerformanceCounter[] ListInstances(PerformanceCounterCategory category, string instanceName)
        {
            // Console.WriteLine("    {0}", instanceName);
            PerformanceCounter[] counters = category.GetCounters(instanceName);

            //foreach (PerformanceCounter counter in counters)
            //{
            //    Console.WriteLine("{0} \t {1} \t {2} \t {3}", counter.CategoryName,counter.CounterName,counter.InstanceName,counter.NextValue());
            //}

            return counters;
        }
    }
}
