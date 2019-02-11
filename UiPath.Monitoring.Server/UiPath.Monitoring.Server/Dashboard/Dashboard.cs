using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UiPath.Monitoring.Server.Dashboard
{
    class AllData
    {
        public List<RealTimeDashboardDetail> exeData { get; set; }
        public Dictionary<string, List<Info>> systemInfo { get; set; }
    }
    class Dashboard
    {      
        public static string getDashboard()
        {
            var ls = new List<RealTimeDashboardDetail>();
            foreach (KeyValuePair<string, DashboardDetails> exe in Program.exeDetailDict)
            {
                RealTimeDashboardDetail temp_RealTimeDashboardDetail = new RealTimeDashboardDetail();
                temp_RealTimeDashboardDetail.time = DateTime.Now;
                temp_RealTimeDashboardDetail.processName = exe.Value.getProcessName();
                temp_RealTimeDashboardDetail.dllInfo = exe.Value.getDllInfo();
                temp_RealTimeDashboardDetail.commitSize = exe.Value.getCommitSize();
                temp_RealTimeDashboardDetail.workingSet = exe.Value.getWorkingSet();
                temp_RealTimeDashboardDetail.privateWorkingSet = exe.Value.getPrivateWorkingSet();
                temp_RealTimeDashboardDetail.peakWorkingSet = exe.Value.getPeakWorkingSet();
                temp_RealTimeDashboardDetail.virtualMemorySize = exe.Value.getVirtualMemorySize();
                temp_RealTimeDashboardDetail.total_cpu = exe.Value.getTotalCPU();
                temp_RealTimeDashboardDetail.process_cpu = exe.Value.getProcessCPU();
                temp_RealTimeDashboardDetail.IOData = exe.Value.getIOData();
                temp_RealTimeDashboardDetail.exeFolder = exe.Value.getExeFolder();
                
                ls.Add(temp_RealTimeDashboardDetail);
            }

            Dictionary<string, List<Info>> temp = new Dictionary<string, List<Info>>();
            temp.Add("System", getInfoFromPerfCounterList(Program.systemInfo.getSystemPerfCounter()));
            temp.Add("CPU", getInfoFromPerfCounterList(Program.systemInfo.getCPUPerfCounter()));
            temp.Add("Network", getInfoFromPerfCounterList(Program.systemInfo.getNetworkPerfCounter()));
            temp.Add("OS", Program.systemInfo.getOSDetails());
            temp.Add("Memory", getInfoFromPerfCounterList(Program.systemInfo.getMemoryPerfCounter()));
            AllData ad = new AllData();
            ad.exeData = ls;
            ad.systemInfo = temp;


            string jsonData = JsonConvert.SerializeObject(ad);

            return jsonData;
        }

        public static List<Info> getInfoFromPerfCounterList(List<PerformanceCounter> ls_pc)
        {
            if (ls_pc == null)
                return null;
            else
            {
                List<Info> systemInfo = new List<Info>();
                foreach (PerformanceCounter pc in ls_pc)
                {
                    systemInfo.Add(
                        new Info
                        {
                            Property = pc.CounterName,
                            Value = pc.NextValue().ToString()
                        }
                        );
                }
                return systemInfo;
            }
        }
    }
}
