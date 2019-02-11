using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UiPath.Monitoring.Server
{
    class DashboardDetails
    {
        Process proc;
        List<DLLInfo> dllInfo;
        double commitSize;
        double workingSet;
        double privateWorkingSet;
        double peakWorkingSet;
        double virtualMemorySize;
        PerformanceCounter total_cpu;
        PerformanceCounter process_cpu;
        PerformanceCounter IOData;
        string exeFolder;

        PerformanceCounter WSPCounter;
        

        public DashboardDetails(Process proc)
        {
            this.proc = proc;
            //dllInfo = GetDll();
            //RAMUtilisation(proc);
            total_cpu = new PerformanceCounter("Process", "% Processor Time", "_Total");
            process_cpu = new PerformanceCounter("Process", "% Processor Time", proc.ProcessName);
            IOData = new PerformanceCounter("Process", "IO Data Bytes/sec", proc.ProcessName);

            WSPCounter = new PerformanceCounter("Process", "Working Set - Private", proc.ProcessName);
        }

        public string getProcessName()
        {
            return proc.ProcessName;
        }
        public List<DLLInfo> getDllInfo()
        {
            return GetDll();
        }
        public double getCommitSize()
        {
            return proc.PagedMemorySize64 / (1024 * 1024);
        }
        public double getWorkingSet()
        {
            return proc.WorkingSet64 / (1024 * 1024);
        }
        public double getPrivateWorkingSet()
        {
            return WSPCounter.RawValue / (1024 * 1024);
        }
        public double getPeakWorkingSet()
        {
            return proc.PeakWorkingSet64 / (1024 * 1024);
        }
        public double getVirtualMemorySize()
        {
            return proc.VirtualMemorySize64 / (1024 * 1024);
        }
        public float getTotalCPU()
        {
            return total_cpu.NextValue();
        }
        public float getProcessCPU()
        {
            return process_cpu.NextValue();
        }
        public float getIOData()
        {
            return IOData.NextValue();
        }
        public string getExeFolder()
        {
            return System.IO.Path.GetDirectoryName(proc.MainModule.FileName);
        }
        

        private List<DLLInfo> GetDll()
        {
            Process toMonitor = this.proc;

            List<DLLInfo> dlls = new List<DLLInfo>();

            foreach (ProcessModule module in toMonitor.Modules)
            {
                dlls.Add(
                    new DLLInfo
                    {
                        Name = module.ModuleName,
                        Path = module.FileName
                    }
                    );
            }
            return dlls;
        }
    }

    public class DLLInfo
    {
        public String Name { get; set; }
        public String Path { get; set; }
    }

    public class Info
    {
        public String Property { get; set; }
        public String Value { get; set; }
    }
}
