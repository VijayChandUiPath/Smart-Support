using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UiPath.Monitoring.Server
{
    [HubName("ServerHub")]
    public class ServerHub : Hub
    {
        public void Server(string cmd)
        {
            Console.WriteLine("Command received : {0}", cmd);

            Commands command;
            Enum.TryParse(cmd, out command);

            switch (command)
            {
                case Commands.Dashboard:
                    {
                        Console.WriteLine("Command received in server : {0}", command);
                        string jsonData = getDashboard();
                        Clients.All.ReceiveDashboardData(jsonData);
                        break;
                    }

                case Commands.ConfigFiles:
                    {
                        Console.WriteLine("Command received in server : {0}", command);
                        string jsonData = getAllConfigFiles();
                        Clients.All.ReceiveConfigFilesData(jsonData);
                        break;
                    }
                case Commands.Ping:
                    {
                        Console.WriteLine("Command received in server : {0}", command);
                        Clients.All.ReceivePong("Pong");
                        Console.WriteLine("Command Sent : Pong");
                        break;
                    }
                case Commands.SaveConfigFile:
                    {
                        Console.WriteLine("Command received in server : {0}", command);
                        Clients.All.GetConfData("Send file");
                        break;
                    }
                case Commands.EVTAppLog:
                    {
                        int evtLogCount;
                        string eventLogName = "Application";
                        string sourceName = "UiPath";
                        string machineName = System.Environment.MachineName;

                        EventLog eventLog = new EventLog();
                        eventLog.Log = eventLogName;
                        eventLog.Source = sourceName;
                        eventLog.MachineName = machineName;

                        var result = (from EventLogEntry elog in eventLog.Entries
                                      where (elog.TimeGenerated >= DateTime.Now.AddDays(-5))
                                      where (elog.Source.ToString().Equals("UiPath") || elog.Source.ToString().Equals("UiRobotSvc") || elog.Source.ToString().Equals(".NET Runtime") || elog.Source.ToString().Equals("Application Error"))
                                      orderby elog.TimeGenerated descending
                                      select elog).ToList();

                        evtLogCount = result.Count;
                        List<string> EVTMSGDetails = new List<string>();
                        List<string> EVTSRCDetails = new List<string>();
                        List<string> EVTTimeDetails = new List<string>();
                        List<string> EVTEntryType = new List<string>();

                        for (int i = 0; i < evtLogCount; i++)
                        {
                            EVTMSGDetails.Add(result[i].Message);
                            EVTSRCDetails.Add(result[i].Source);
                            EVTTimeDetails.Add(Convert.ToString(result[i].TimeGenerated));
                            EVTEntryType.Add(Convert.ToString(result[i].EntryType));
                        }

                        var ls = new List<List<string>> { EVTSRCDetails, EVTTimeDetails, EVTEntryType, EVTMSGDetails };


                        string jsonData = JsonConvert.SerializeObject(ls);
                        Clients.All.ReceiveEVTAppLogData(jsonData);
                        break;
                    }
                case Commands.EVTSecLog:
                    {
                        int evtLogCount;
                        string machineName = System.Environment.MachineName;

                        EventLog eventLog = new EventLog("Security");
                        //eventLog.Log = eventLogName;
                        eventLog.MachineName = machineName;

                        var result = (from EventLogEntry elog in eventLog.Entries
                                      where (elog.TimeGenerated >= DateTime.Now.AddDays(-5))
                                      orderby elog.TimeGenerated descending
                                      select elog).ToList();

                        evtLogCount = result.Count;
                        List<string> EVTMSGDetails = new List<string>();
                        List<string> EVTSRCDetails = new List<string>();
                        List<string> EVTTimeDetails = new List<string>();
                        List<string> EVTEntryType = new List<string>();

                        //int maxRecords = 1000;
                        if (evtLogCount > 1000)
                        {
                            evtLogCount = 1000;
                        }

                        for (int i = 0; i < evtLogCount; i++)
                        {
                            // Console.WriteLine();
                            // Console.WriteLine(result[i].Message);
                            EVTMSGDetails.Add(result[i].Message);
                            EVTSRCDetails.Add(result[i].Source);
                            EVTTimeDetails.Add(Convert.ToString(result[i].TimeGenerated));
                            EVTEntryType.Add(Convert.ToString(result[i].EntryType));
                        }

                        var ls = new List<List<string>> { EVTSRCDetails, EVTTimeDetails, EVTEntryType, EVTMSGDetails };
                        string jsonData = JsonConvert.SerializeObject(ls);
                        Clients.All.ReceiveEVTSecLogData(jsonData);
                        break;
                    }
                case Commands.EnableTrace:
                    {
                        string path = @"C:\Program Files (x86)\UiPath\Studio";
                        if (Directory.Exists(path))
                        {
                            string command1 = "UiRobot.exe --enableLowLevel";
                            try
                            {
                                var proc1 = new ProcessStartInfo();
                                // string anyCommand;
                                proc1.UseShellExecute = true;

                                proc1.WorkingDirectory = @"C:\Program Files (x86)\UiPath\Studio";

                                proc1.FileName = "cmd.exe";
                                proc1.Verb = "runas";
                                proc1.Arguments = "/c " + command1;
                                proc1.WindowStyle = ProcessWindowStyle.Hidden;
                                Process.Start(proc1);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine("Error : Make sure UiPath is installed! : " +  ex.Message);                               

                            }
                        }
                            break;
                    }
                case Commands.DisableTrace:
                    {
                        string path = @"C:\Program Files (x86)\UiPath\Studio";
                        if (Directory.Exists(path))
                        {
                            string command1 = "UiRobot.exe --disableLowLevel";
                            try
                            {
                                var proc1 = new ProcessStartInfo();
                                // string anyCommand;
                                proc1.UseShellExecute = true;

                                proc1.WorkingDirectory = @"C:\Program Files (x86)\UiPath\Studio";

                                proc1.FileName = "cmd.exe";
                                proc1.Verb = "runas";
                                proc1.Arguments = "/c " + command1;
                                proc1.WindowStyle = ProcessWindowStyle.Hidden;
                                Process.Start(proc1);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine("Error : Make sure UiPath is installed! : " +  ex.Message);
                            }
                        }
                            break;
                    }
                case Commands.DoNothing:
                    {
                        Console.WriteLine("Command received in server : {0}", command);
                        Clients.All.ReceiveCommand("Command sent to client : " + command);
                        break;
                    }
                case Commands.Stop:
                    {
                        Console.WriteLine("Command received in server : {0}", command);
                        Clients.All.StopConnection("Command sent to stop connection");
                        break;
                    }
            }
        }

        static string getDashboard()
        {
            return Dashboard.Dashboard.getDashboard();
        }

        static string getAllConfigFiles()
        {
            List<ConfigFileDetails> ls = new List<ConfigFileDetails>();
            var ext = new List<string> { ".config", ".Config", ".settings" };

            foreach (KeyValuePair<string, DashboardDetails> exe in Program.exeDetailDict)
            {
                List<string> myFiles = (Directory.GetFiles(exe.Value.getExeFolder(), "*.*", SearchOption.AllDirectories).Where(s => ext.Contains(System.IO.Path.GetExtension(s)))).ToList();
                foreach (string file in myFiles)
                {
                    //var temp = new Dictionary<string, string> { { file, System.IO.File.ReadAllText(file) } };
                    ls.Add(new ConfigFileDetails()
                    {
                        fileName = file,
                        fileContent = System.IO.File.ReadAllText(file)
                    });

                }
            }
            string jsonData = JsonConvert.SerializeObject(ls);
            return jsonData;
        }
        enum Commands
        {
            DoNothing,
            Dashboard,
            ConfigFiles,
            SaveConfigFile,
            EVTAppLog,
            EVTSecLog,
            EnableTrace,
            DisableTrace,
            Ping,
            Stop
        }
    }
}
