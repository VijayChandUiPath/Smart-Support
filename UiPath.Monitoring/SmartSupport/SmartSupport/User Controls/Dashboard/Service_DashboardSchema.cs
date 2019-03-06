using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartSupport.User_Controls.Dashboard
{
    class Service_DashboardSchema
    {
        public List<RealTimeDashboardDetail> exeData { get; set; }
        public Dictionary<string, List<Info>> systemInfo { get; set; }
    }
    class RealTimeDashboardDetail
    {
        public string time;
        public string processName { get; set; }
        public List<DLLInfo> dllInfo { get; set; }
        public double commitSize { get; set; }
        public double workingSet { get; set; }
        public double privateWorkingSet { get; set; }
        public double peakWorkingSet { get; set; }
        public double virtualMemorySize { get; set; }
        public float total_cpu { get; set; }
        public float process_cpu { get; set; }
        public float IOData { get; set; }
        public string exeFolder { get; set; }
    }
}
