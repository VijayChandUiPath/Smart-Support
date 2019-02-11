using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UiPath.Monitoring.Server.Dashboard
{
    class SystemInfo
    {
        List<PerformanceCounter> systemPerfCounter;
        List<PerformanceCounter> cpuPerfCounter;
        List<PerformanceCounter> networkPerfCounter;
        List<Info> osPerfCounter;
        List<PerformanceCounter> memoryPerfCounter;

        public SystemInfo()
        {
            systemPerfCounter = GetPerformanceCounterList.ListCounters(new string[] { "System" });
            cpuPerfCounter = GetPerformanceCounterList.ListCounters(new string[] { "Processor", "Processor Information" });
            networkPerfCounter = GetPerformanceCounterList.ListCounters(new string[] { "Network Adapter", "Network Interface" });
            memoryPerfCounter = GetPerformanceCounterList.ListCounters(new string[] { "Memory" });
            OperatingSystem os = Environment.OSVersion;
            Version ver = os.Version;
            #region OS Details
            osPerfCounter = new List<Info> {
                new Info
                {
                    Property = "OS Version",
                    Value = os.Version.ToString()
                },
                new Info
                {
                    Property = "OS Platoform",
                    Value = os.Platform.ToString()
                },
                new Info
                {
                    Property = "OS SP",
                    Value = os.ServicePack.ToString()
                },
                new Info
                {
                    Property = "OS Version String",
                    Value = os.VersionString.ToString()
                },
                new Info
                {
                    Property = "Major version",
                    Value = ver.Major.ToString()
                },
                new Info
                {
                    Property = "Major Revision",
                    Value = ver.MajorRevision.ToString()
                },
                new Info
                {
                    Property = "Minor version",
                    Value = ver.Minor.ToString()
                },
                new Info
                {
                    Property = "Minor Revision",
                    Value = ver.MinorRevision.ToString()
                },
                new Info
                {
                    Property = "Build",
                    Value = ver.Build.ToString()
                }
            };
            #endregion
        }

        public List<PerformanceCounter> getSystemPerfCounter()
        {
            return systemPerfCounter;
        }
        public List<PerformanceCounter> getCPUPerfCounter()
        {
            return cpuPerfCounter;
        }
        public List<PerformanceCounter> getNetworkPerfCounter()
        {
            return networkPerfCounter;
        }
        public List<PerformanceCounter> getMemoryPerfCounter()
        {
            return memoryPerfCounter;
        }
        public List<Info> getOSDetails()
        {
            return osPerfCounter;
        }
    }
}
