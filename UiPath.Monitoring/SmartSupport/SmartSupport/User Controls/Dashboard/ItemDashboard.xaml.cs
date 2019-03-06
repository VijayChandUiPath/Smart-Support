using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Collections.ObjectModel;
using LiveCharts;
using LiveCharts.Wpf;
using System.Diagnostics;
using System.Windows.Threading;
using LiveCharts.Defaults;
using LiveCharts.Configurations;
using SmartSupport.User_Controls.Error;
using SmartSupport.User_Controls.Settings;
using Newtonsoft.Json;
using System.IO;
using System.Collections;
using System.Threading;
using Microsoft.AspNet.SignalR.Client;

namespace SmartSupport.User_Controls.Dashboard
{
    /// <summary>
    /// Interaction logic for DashBoard.xaml
    /// </summary>
    public partial class ItemDashboard : UserControl
    {
        private static ObservableCollection<Info> systemInfo = new ObservableCollection<Info>();
        DispatcherTimer dt_Dll;
        DispatcherTimer dt_system_info;
        DispatcherTimer dt_graph;
        DispatcherTimer dt_exeMonitor;
        DispatcherTimer dt_connectionMontor;
        static List<PerformanceCounter> Perf_System;
        PerformanceCounter total_cpu;
        PerformanceCounter process_cpu;
        PerformanceCounter IOData;
        PerformanceCounter WSPCounter;
        UserControl ErrorUSC = null;
        bool graphErrorFlag = false;
        bool dllErrorFlag = false;
        static string systemInfoChecked = "System";
        static Service_DashboardSchema dashboardData = new Service_DashboardSchema();
        
        public Func<double, string> Formatter { get; set; }
        public ChartValues<ObservableValue> Values { get; set; }
        public Brush DangerBrush { get; set; }
        public CartesianMapper<ObservableValue> Mapper { get; set; }
        public Func<ChartPoint, string> PointLabel { get; set; }
        public SeriesCollection sc { get; set; }
        public Func<double, string> YFormatter { get; set; }


        public ItemDashboard()
        {
            InitializeComponent();

            // Initialise chart
            try
            {                
                #region initialize charts
                Values = new ChartValues<ObservableValue>
                {
                    new ObservableValue(0),
                    new ObservableValue(0),
                    new ObservableValue(0),
                    new ObservableValue(0),
                    new ObservableValue(0),
                    new ObservableValue(0)
                };

                Mapper = Mappers.Xy<ObservableValue>()
                    .X((item, index) => index)
                    .Y(item => item.Value)
                    .Fill(item => item.Value > 200 ? DangerBrush : null)
                    .Stroke(item => item.Value > 200 ? DangerBrush : null);

                Formatter = x => x + " ms";
                DangerBrush = new SolidColorBrush(Color.FromRgb(238, 83, 80));
                PointLabel = chartPoint =>
                string.Format("{0} ({1:P})", chartPoint.Y, chartPoint.Participation);

                sc = new SeriesCollection
                {
                    new LineSeries
                    {
                        Title = "Commit Size",
                        Values = new ChartValues<double> {0, 0, 0 }
                    },
                    new LineSeries
                    {
                        Title = "Working Set",
                        Values = new ChartValues<double> {0, 0, 0 },
                        //PointGeometry = null
                    },
                    new LineSeries
                    {
                        Title = "Private Working Set",
                        Values = new ChartValues<double> {0, 0, 0 },
                        //PointGeometry = null
                    },
                    new LineSeries
                    {
                        Title = "Peak Working Set",
                        Values = new ChartValues<double> {0, 0, 0 },
                        //PointGeometry = null
                    },
                    new LineSeries
                    {
                        Title = "Private Working Set",
                        Values = new ChartValues<double> {0, 0, 0 },
                        //PointGeometry = null
                    },
                    new LineSeries
                    {
                        Title = "Virtual Memory",
                        Values = new ChartValues<double> {0, 0, 0 },
                        //PointGeometry = null
                    }
                };

                YFormatter = value => value.ToString();
                DataContext = this;

                #endregion

            }
            catch (Exception ex)
            {

            }

            //monitorConnection(null, null);
            dt_connectionMontor = new DispatcherTimer();
            dt_connectionMontor.Interval = TimeSpan.FromSeconds(2);
            dt_connectionMontor.Tick += monitorConnection;
            dt_connectionMontor.Start();

            dt_exeMonitor = new DispatcherTimer();
            dt_exeMonitor.Interval = TimeSpan.FromSeconds(2);
            dt_exeMonitor.Tick += monitorExeRunning;
            dt_exeMonitor.Start();

            #region do not delete
            //if (connected)
            //{


            //    // Encode the data string into a byte array.    
            //    byte[] msg = Encoding.ASCII.GetBytes("Dashboard");

            //    // Send the data through the socket.    
            //    int bytesSent = MainWindow.sender.Send(msg);



            //    //dt_serviceData = new DispatcherTimer();
            //    //dt_serviceData.Interval = TimeSpan.FromSeconds(1);
            //    //dt_serviceData.Tick += serviceDashboardData;
            //    //dt_serviceData.Start();

            //    serviceDashboardData(null, null);

            //}
            //else
            //{

            //    #region Get UiPath Processes
            //    Process[] processlist = Process.GetProcesses();
            //    foreach (Process theprocess in processlist)
            //    {
            //        if (theprocess.ProcessName.Contains("UiPath"))
            //        {
            //            string fileName = theprocess.MainModule.FileName;
            //            FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(fileName);
            //            string company = fvi.CompanyName;
            //            if (company.Contains("UiPath"))
            //                ComboExe.Items.Add(theprocess.ProcessName);
            //        }
            //    }
            //    if (ComboExe.Items.Count > 0)
            //    {
            //        ComboExe.SelectedIndex = 0;
            //    }
            //    else
            //    {
            //        ErrorUSC = new MainWindowError("Error : No UiPath process is running...", "Please run the UiPath service from service manager", false);
            //        DashboardUserControl.Children.Clear();
            //        DashboardUserControl.Children.Add(ErrorUSC);
            //        return;
            //    }
            //    #endregion

            //    #region charts
            //    Values = new ChartValues<ObservableValue>
            //{
            //    new ObservableValue(0),
            //    new ObservableValue(0),
            //    new ObservableValue(0),
            //    new ObservableValue(0),
            //    new ObservableValue(0),
            //    new ObservableValue(0)
            //};

            //    Mapper = Mappers.Xy<ObservableValue>()
            //        .X((item, index) => index)
            //        .Y(item => item.Value)
            //        .Fill(item => item.Value > 200 ? DangerBrush : null)
            //        .Stroke(item => item.Value > 200 ? DangerBrush : null);

            //    Formatter = x => x + " ms";
            //    DangerBrush = new SolidColorBrush(Color.FromRgb(238, 83, 80));
            //    PointLabel = chartPoint =>
            //    string.Format("{0} ({1:P})", chartPoint.Y, chartPoint.Participation);

            //    sc = new SeriesCollection
            //{
            //    new LineSeries
            //    {
            //        Title = "Commit Size",
            //        Values = new ChartValues<double> {0, 0, 0 }
            //    },
            //    new LineSeries
            //    {
            //        Title = "Working Set",
            //        Values = new ChartValues<double> {0, 0, 0 },
            //        //PointGeometry = null
            //    },
            //    new LineSeries
            //    {
            //        Title = "Private Working Set",
            //        Values = new ChartValues<double> {0, 0, 0 },
            //        //PointGeometry = null
            //    },
            //    new LineSeries
            //    {
            //        Title = "Peak Working Set",
            //        Values = new ChartValues<double> {0, 0, 0 },
            //        //PointGeometry = null
            //    },
            //    new LineSeries
            //    {
            //        Title = "Private Working Set",
            //        Values = new ChartValues<double> {0, 0, 0 },
            //        //PointGeometry = null
            //    },
            //    new LineSeries
            //    {
            //        Title = "Virtual Memory",
            //        Values = new ChartValues<double> {0, 0, 0 },
            //        //PointGeometry = null
            //    }
            //};

            //    YFormatter = value => value.ToString();
            //    DataContext = this;

            //    #endregion

            //}
            #endregion
        }

        private void monitorConnection(object sender, EventArgs e)
        {
            
            if (MainWindow.connected)
            {                
                recieveDataFromService();               
            }
            //else
            //{
            //    monitorExeRunning(null, null);
            //}
        }

        private void recieveDataFromService()
        {
            MainWindow._hub.On("ReceiveDashboardData", x => {
                File.AppendAllText(@"log.txt", x + Environment.NewLine);
                dashboardData = JsonConvert.DeserializeObject<Service_DashboardSchema>(x);
            });
            MainWindow._hub.Invoke("Server", "Dashboard").Wait();
            

            MainWindow.command = "Dashboard";
            svcTime.Content = dashboardData.exeData[0].time;
            #region old code
            //// Encode the data string into a byte array.    
            //byte[] msg = Encoding.ASCII.GetBytes("Dashboard");

            //// Send the data through the socket.    
            //int bytesSent = MainWindow.sender.Send(msg);
            //byte[] bytes = new byte[1024 * 1024];  // Reserving memory of 1 MB. Data more than this will be lost and exception will occur while De-serializing 


            //try
            //{
            //    // Receive the response from the remote device.
            //    MainWindow.sender.ReceiveTimeout = 3000;    
            //    int bytesRec = MainWindow.sender.Receive(bytes);
            //    string data = Encoding.ASCII.GetString(bytes, 0, bytesRec);
            //    //Console.WriteLine(data);
            //    File.AppendAllText(@"log.txt", data + Environment.NewLine);

            //    lock(dashboardData)
            //    {
            //        dashboardData = JsonConvert.DeserializeObject<Service_DashboardSchema>(data);
            //    }

            //    MainWindow.command = "Dashboard";
            //    svcTime.Content = dashboardData.exeData[0].time;
            //    //getExeNamesFromService();

            //}
            //catch (Exception ex)
            //{
            //    Console.WriteLine(ex.Message);
            //    MainWindow.connected = false;
            //    MainWindow.command = "DoNothing";
            //}

            #endregion
        }

        private void getExeNamesFromService()
        {
            string[] exeRunning = new string[dashboardData.exeData.Count];
            int count = 0;
            List < RealTimeDashboardDetail > exeData = dashboardData.exeData;
            for(int i = 0; i< dashboardData.exeData.Count; i++)
            {
                count++;
                exeRunning[i] = dashboardData.exeData[i].processName;
            }

            if(count > 0)
            {
                // ComboExe already contains some exe
                if(ComboExe.Items.Count > 0)
                {
                    // Check if populated exes and the new exe are same
                    string[] comboItems = new string[ComboExe.Items.Count];
                    string selectedEXE = ComboExe.SelectedItem.ToString();
                    for (int i = 0; i < ComboExe.Items.Count; i++)
                    {
                        comboItems[i] = ComboExe.Items[i].ToString();
                    }

                    string[] exeFromService = new string[dashboardData.exeData.Count];
                    for (int i = 0; i < dashboardData.exeData.Count; i++)
                    {
                        exeFromService[i] = dashboardData.exeData[i].processName;
                    }

                    // check if both the arrays are same
                    bool areEqual = (comboItems as IStructuralEquatable).Equals(exeFromService, EqualityComparer<string>.Default);

                    if(areEqual)
                    {
                        // Keep Previous Exe selected
                        // already selected so need to select again
                    }
                    else
                    {
                        // Check if previously selected exe is present or not
                        if(exeFromService.Contains(selectedEXE))
                        {                            
                            ComboExe.Items.Clear();
                            int index = 0;                            

                            for(int i = 0; i < exeData.Count; i++)
                            {
                                ComboExe.Items.Add(exeData[i].processName);
                                if (selectedEXE == exeData[i].processName)
                                {
                                    index = i;
                                } 
                            }

                            ComboExe.SelectedIndex = index;
                            
                        }
                        else
                        {
                            // Populate all exe on UI
                            ComboExe.Items.Clear();
                            for (int i = 0; i < exeData.Count; i++)
                            {
                                ComboExe.Items.Add(exeData[i].processName);                         
                            }

                            ComboExe.SelectedIndex = 0;
                        }
                    }
                }
                else
                {
                    // Populate all exe on UI
                    ComboExe.Items.Clear();
                    for (int i = 0; i < exeData.Count; i++)
                    {
                        ComboExe.Items.Add(exeData[i].processName);
                    }

                    ComboExe.SelectedIndex = 0;                    
                }

                // Start Dll, System Info and Graph
                SystemInfo(null, null);
                GridDll.ItemsSource = GetDll(ComboExe.SelectedItem.ToString());
                ChangeGraph(null, null);


            }
            else
            {
                // SHOW ERROR MESSAGE
                getExeNamesFromService();
            }
        }

        private void serviceDashboardData(object sender, EventArgs e)
        {
            byte[] bytes = new byte[1024 * 1024];  // Reserving memory of 1 MB. Data more than this will be lost and exception will occur while De-serializing 


            // Receive the response from the remote device.    
            int bytesRec = MainWindow.sender.Receive(bytes);
            string data = Encoding.ASCII.GetString(bytes, 0, bytesRec);
            //Console.WriteLine(data);
            File.AppendAllText(@"log.txt", data + Environment.NewLine);

            dashboardData = JsonConvert.DeserializeObject<Service_DashboardSchema>(data);

            if (ComboExe.Items.Count < 1)
            {
                #region Adding exes

                List<RealTimeDashboardDetail> exeData = dashboardData.exeData;
                foreach (var item in exeData)
                {
                    ComboExe.Items.Add(item.processName);
                }

                if (ComboExe.Items.Count > 0)
                {
                    ComboExe.SelectedIndex = 0;
                }
                else
                {
                    ErrorUSC = new MainWindowError("Error : No UiPath process is running...", "Please run the UiPath service from service manager", false);
                    DashboardUserControl.Children.Clear();
                    DashboardUserControl.Children.Add(ErrorUSC);
                    return;
                }
                #endregion
            }
            else
            {
                // Check if the exe is up or has gone down
                string[] comboItems = new string[ComboExe.Items.Count];
                for(int i = 0; i < ComboExe.Items.Count;i++)
                {
                    comboItems[i] = ComboExe.Items[i].ToString();
                }

                string[] exeFromService = new string[dashboardData.exeData.Count];
                for (int i = 0; i < dashboardData.exeData.Count; i++)
                {
                    exeFromService[i] = dashboardData.exeData[i].processName;
                }

                // check if both the arrays are same
                bool areEqual = (comboItems as IStructuralEquatable).Equals(exeFromService, EqualityComparer<string>.Default);

                if(! areEqual)
                {
                    List<RealTimeDashboardDetail> exeData = dashboardData.exeData;
                    ComboExe.Items.Clear();
                    foreach (var item in exeData)
                    {
                        ComboExe.Items.Add(item.processName);
                    }

                    if (ComboExe.Items.Count > 0)
                    {
                        ComboExe.SelectedIndex = 0;
                    }
                    else
                    {
                        ErrorUSC = new MainWindowError("Error : No UiPath process is running...", "Please run the UiPath service from service manager", false);
                        DashboardUserControl.Children.Clear();
                        DashboardUserControl.Children.Add(ErrorUSC);
                        return;
                    }
                }

            }
        }

        private void loadFirstTime()
        {
            #region Get UiPath Processes
            Console.WriteLine("Loading First Time");
            Process[] processlist = Process.GetProcesses();
            foreach (Process theprocess in processlist)
            {
                if (theprocess.ProcessName.Contains("UiPath"))
                {
                    string fileName = theprocess.MainModule.FileName;
                    FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(fileName);
                    string company = fvi.CompanyName;
                    if (company.Contains("UiPath"))
                        ComboExe.Items.Add(theprocess.ProcessName);
                }
            }
            if (ComboExe.Items.Count > 0)
            {
                ComboExe.SelectedIndex = 0;
            }
            else
            {
                ErrorUSC = new MainWindowError("Error : No UiPath process is running...", "Please run the UiPath service from service manager", false);
                DashboardUserControl.Children.Clear();
                DashboardUserControl.Children.Add(ErrorUSC);
                return;
            }
            #endregion

            #region charts
            Values = new ChartValues<ObservableValue>
            {
                new ObservableValue(0),
                new ObservableValue(0),
                new ObservableValue(0),
                new ObservableValue(0),
                new ObservableValue(0),
                new ObservableValue(0)
            };

            Mapper = Mappers.Xy<ObservableValue>()
                .X((item, index) => index)
                .Y(item => item.Value)
                .Fill(item => item.Value > 200 ? DangerBrush : null)
                .Stroke(item => item.Value > 200 ? DangerBrush : null);

            Formatter = x => x + " ms";
            DangerBrush = new SolidColorBrush(Color.FromRgb(238, 83, 80));
            PointLabel = chartPoint =>
            string.Format("{0} ({1:P})", chartPoint.Y, chartPoint.Participation);

            sc = new SeriesCollection
            {
                new LineSeries
                {
                    Title = "Commit Size",
                    Values = new ChartValues<double> {0, 0, 0 }
                },
                new LineSeries
                {
                    Title = "Working Set",
                    Values = new ChartValues<double> {0, 0, 0 },
                    //PointGeometry = null
                },
                new LineSeries
                {
                    Title = "Private Working Set",
                    Values = new ChartValues<double> {0, 0, 0 },
                    //PointGeometry = null
                },
                new LineSeries
                {
                    Title = "Peak Working Set",
                    Values = new ChartValues<double> {0, 0, 0 },
                    //PointGeometry = null
                },
                new LineSeries
                {
                    Title = "Private Working Set",
                    Values = new ChartValues<double> {0, 0, 0 },
                    //PointGeometry = null
                },
                new LineSeries
                {
                    Title = "Virtual Memory",
                    Values = new ChartValues<double> {0, 0, 0 },
                    //PointGeometry = null
                }
            };

            YFormatter = value => value.ToString();
            DataContext = this;

            #endregion
        }

        private void monitorExeRunning(object sender, EventArgs e)
        {
            if(MainWindow.connected && MainWindow.command == "Dashboard")
            {
                getExeNamesFromService();
            }
            else
            {
                // First time loading
                if (ComboExe.Items.Count < 1)
                {
                    Console.WriteLine("First time loading");
                    loadFirstTime();
                }
                else
                {
                    // Check if the exe is up or has gone down
                    string[] comboItems = new string[ComboExe.Items.Count];
                    for (int i = 0; i < ComboExe.Items.Count; i++)
                    {
                        comboItems[i] = ComboExe.Items[i].ToString();
                    }

                    List<string> currentExe = new List<string>();
                    Process[] processlist = Process.GetProcesses();
                    foreach (Process theprocess in processlist)
                    {
                        if (theprocess.ProcessName.Contains("UiPath"))
                        {
                            string fileName = theprocess.MainModule.FileName;
                            FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(fileName);
                            string company = fvi.CompanyName;
                            if (company.Contains("UiPath"))
                                currentExe.Add(theprocess.ProcessName);
                        }
                    }

                    // check if both the arrays are same
                    bool areEqual = (comboItems as IStructuralEquatable).Equals(currentExe.ToArray(), EqualityComparer<string>.Default);
                    if (!areEqual)
                        loadFirstTime();
                } 
            }
        }

        private void DashboardUserControl_Loaded(object sender, RoutedEventArgs e)
        {

            if (MainWindow.connected && MainWindow.command == "Dashboard")
            {
                //getExeNamesFromService();
            }
            else
            {
                try
                {
                    GridDll.ItemsSource = GetDll(ComboExe.SelectedValue.ToString());
                }
                catch (Exception ex)
                {
                    if (ComboExe.Items.Count == 0)
                    {
                        ComboExe.Items.Clear();
                        ErrorUSC = new MainWindowError("Error : Problem in loading dlls", ex.Message, false);
                    }
                    else
                        ErrorUSC = new MainWindowError("Error : Problem in loading " + ComboExe.SelectedValue.ToString() + " dlls", ex.Message, false);
                    //Dll.Children.Add(ErrorUSC);
                }

                // Get System Information
                try
                {
                    Perf_System = GetPerformanceCounterList.ListCounters(new string[] { "System" });
                    dt_system_info = new DispatcherTimer();
                    dt_system_info.Interval = TimeSpan.FromSeconds(1);
                    dt_system_info.Tick += SystemInfo;
                    dt_system_info.Start();
                }
                catch (Exception ex)
                {
                    ErrorUSC = new MainWindowError("Error : Problem in loading System Informations", ex.Message, false);
                    EnvValues.Children.Add(ErrorUSC);
                }

                // Graph
                try
                {
                    dt_graph = new DispatcherTimer();
                    dt_graph.Interval = TimeSpan.FromSeconds(1);
                    dt_graph.Tick += ChangeGraphFromService;
                    dt_graph.Start();

                }
                catch (Exception ex)
                {
                    ErrorUSC = new MainWindowError("Error : Problem in loading Graphs", ex.Message, false);
                    Graph.Children.Add(ErrorUSC);
                } 
            }
        }

        private static ObservableCollection<DLLInfo> GetDll(string exe)
        {
            ObservableCollection<DLLInfo> dlls = new ObservableCollection<DLLInfo>();

            if (MainWindow.connected && MainWindow.command == "Dashboard")
            {
                List<DLLInfo> dllInfo = dashboardData.exeData.Where(s => s.processName == exe).FirstOrDefault().dllInfo;
                dlls = new ObservableCollection<DLLInfo>(dllInfo);
            }
            else
            {
                Process toMonitor = Process.GetProcessesByName(exe).FirstOrDefault();
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
            }
            return dlls;
        }

        private void SystemInfo(object sender, EventArgs e)
        {
            systemInfo.Clear();
            systemInfo = GetSystemInfo();

            if (systemInfo.Count > 5)
            {
                int split = Convert.ToInt32(systemInfo.Count / 2);
                ObservableCollection<Info> split1 = new ObservableCollection<Info>();
                ObservableCollection<Info> split2 = new ObservableCollection<Info>();

                for (int i = 0; i < split; i++)
                {
                    split1.Add(systemInfo[i]);
                }

                for (int i = split; i < systemInfo.Count; i++)
                {
                    split2.Add(systemInfo[i]);
                }

                GridValue1.ItemsSource = split1;
                GridValue2.ItemsSource = split2;
            }
            else
                GridValue1.ItemsSource = systemInfo;
        }

        private static ObservableCollection<Info> GetSystemInfo()
        {
            ObservableCollection<Info> systemInfo = new ObservableCollection<Info>();
            if (Perf_System == null)
                return null;
            else if(MainWindow.connected && MainWindow.command == "Dashboard")
            {
                List<Info> temp = new List<Info>();

                temp.AddRange(dashboardData.systemInfo[systemInfoChecked]);
                
                
                //File.AppendAllText(@"SystemInfoService.txt", JsonConvert.SerializeObject(temp) + Environment.NewLine + "===================================================================");
                systemInfo = new ObservableCollection<Info>(temp);
                //File.AppendAllText(@"SystemInfoUI.txt", JsonConvert.SerializeObject(systemInfo) + Environment.NewLine + "===================================================================");
                return systemInfo;
            }
            else
            {                
                foreach (PerformanceCounter pc in Perf_System)
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

        private void ChangeGraph(object sender, EventArgs e)
        {
            if (MainWindow.connected && MainWindow.command == "Dashboard")
            {
                List<RealTimeDashboardDetail> exeData = dashboardData.exeData;

                // CPU Gauge
                foreach (var item in exeData)
                {
                    if (item.processName == ComboExe.SelectedItem.ToString())
                    {
                        float t = item.total_cpu;
                        float p = item.process_cpu;

                        if (t != 0)
                        {
                            double temp = Math.Round(p / t * 100, 2);
                            gauge_cpu.Value = temp;
                            chip_cpu.Content = temp;
                        }

                        // IO Graph
                        for (int i = 0; i < Values.Count - 1; i++)
                        {
                            Values[i] = Values[i + 1];
                        }
                        Values[Values.Count - 1] = new ObservableValue(item.IOData);


                        // Memory Graph
                        foreach (LineSeries ls in sc)
                        {
                            switch (ls.Title)
                            {
                                case "Commit Size":
                                    {
                                        for (int i = 0; i < ls.Values.Count - 1; i++)
                                        {
                                            ls.Values[i] = ls.Values[i + 1];
                                        }
                                        ls.Values[ls.Values.Count - 1] = item.commitSize;
                                        break;
                                    }
                                case "Working Set":
                                    {
                                        for (int i = 0; i < ls.Values.Count - 1; i++)
                                        {
                                            ls.Values[i] = ls.Values[i + 1];
                                        }
                                        ls.Values[ls.Values.Count - 1] = item.workingSet;
                                        break;
                                    }
                                case "Private Working Set":
                                    {
                                        for (int i = 0; i < ls.Values.Count - 1; i++)
                                        {
                                            ls.Values[i] = ls.Values[i + 1];
                                        }
                                        ls.Values[ls.Values.Count - 1] = item.privateWorkingSet;
                                        break;
                                    }
                                case "Peak Working Set":
                                    {
                                        for (int i = 0; i < ls.Values.Count - 1; i++)
                                        {
                                            ls.Values[i] = ls.Values[i + 1];
                                        }
                                        ls.Values[ls.Values.Count - 1] = item.peakWorkingSet;
                                        break;
                                    }
                                case "Virtual Memory":
                                    {
                                        for (int i = 0; i < ls.Values.Count - 1; i++)
                                        {
                                            ls.Values[i] = ls.Values[i + 1];
                                        }
                                        ls.Values[ls.Values.Count - 1] = item.virtualMemorySize;
                                        break;
                                    }
                                default:
                                    break;
                            }
                        }
                    }
                    
                    DataContext = this;
                }

             
                return;
            }
                
            try
            {
                // CPU Gauge
                float t = total_cpu.NextValue();
                float p = process_cpu.NextValue();
                if (t != 0)
                {
                    double temp = Math.Round(p / t * 100, 2);
                    gauge_cpu.Value = temp;
                    chip_cpu.Content = temp;
                }

                // IO Graph
                for (int i = 0; i < Values.Count - 1; i++)
                {
                    Values[i] = Values[i + 1];
                }
                Values[Values.Count - 1] = new ObservableValue(IOData.NextValue());


                #region memory graph
                Process proc = Process.GetProcessesByName(ComboExe.SelectedValue.ToString()).FirstOrDefault();
                double commit = proc.PagedMemorySize64 / (1024 * 1024);
                double workingSet = proc.WorkingSet64 / (1024 * 1024);
                double privateWorkingSet = WSPCounter.RawValue / (1024 * 1024);
                double peakWorkingSet = proc.PeakWorkingSet64 / (1024 * 1024);
                double virtualMemorySize = proc.VirtualMemorySize64 / (1024 * 1024);

                foreach (LineSeries ls in sc)
                {
                    switch (ls.Title)
                    {
                        case "Commit Size":
                            {
                                for (int i = 0; i < ls.Values.Count - 1; i++)
                                {
                                    ls.Values[i] = ls.Values[i + 1];
                                }
                                ls.Values[ls.Values.Count - 1] = commit;
                                break;
                            }
                        case "Working Set":
                            {
                                for (int i = 0; i < ls.Values.Count - 1; i++)
                                {
                                    ls.Values[i] = ls.Values[i + 1];
                                }
                                ls.Values[ls.Values.Count - 1] = workingSet;
                                break;
                            }
                        case "Private Working Set":
                            {
                                for (int i = 0; i < ls.Values.Count - 1; i++)
                                {
                                    ls.Values[i] = ls.Values[i + 1];
                                }
                                ls.Values[ls.Values.Count - 1] = privateWorkingSet;
                                break;
                            }
                        case "Peak Working Set":
                            {
                                for (int i = 0; i < ls.Values.Count - 1; i++)
                                {
                                    ls.Values[i] = ls.Values[i + 1];
                                }
                                ls.Values[ls.Values.Count - 1] = peakWorkingSet;
                                break;
                            }
                        case "Virtual Memory":
                            {
                                for (int i = 0; i < ls.Values.Count - 1; i++)
                                {
                                    ls.Values[i] = ls.Values[i + 1];
                                }
                                ls.Values[ls.Values.Count - 1] = virtualMemorySize;
                                break;
                            }
                        default:
                            break;
                    }
                }
                DataContext = this;

                #endregion
            }
            catch (Exception ex)
            {
                #region Get UiPath Processes
                Process[] processlist = Process.GetProcesses();
                ComboExe.Items.Clear();
                foreach (Process theprocess in processlist)
                {
                    if (theprocess.ProcessName.Contains("UiPath"))
                    {
                        string fileName = theprocess.MainModule.FileName;
                        FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(fileName);
                        string company = fvi.CompanyName;
                        if (company.Contains("UiPath"))
                            ComboExe.Items.Add(theprocess.ProcessName);
                    }
                }

                #endregion
                if (ComboExe.Items.Count > 0)
                {
                    ComboExe.SelectedIndex = 0;
                }
                else
                {
                    
                    ErrorUSC = new MainWindowError("Error : Problem in loading Graphs", ex.Message, false);
                    //var temp = Graph.Children.OfType<UserControl>().FirstOrDefault();
                    if (!graphErrorFlag)
                    {
                        graphErrorFlag = true;
                        Graph.Children.Clear();
                        Graph.Children.Add(ErrorUSC); 
                    }

                    if (!dllErrorFlag)
                    {
                        dllErrorFlag = true;
                        Dll.Children.Clear();
                        UserControl tempUC = new MainWindowError("Error : Problem in loading dlls", ex.Message, false);
                        //ErrorUSC = new MainWindowError("Error : Problem in loading dlls", ex.Message, false);
                        Dll.Children.Add(tempUC); 
                    }
                }
            }

        }


        private void ChangeGraphFromService(object sender, EventArgs e)
        {

        }
        private void SystemButton_Click(object sender, RoutedEventArgs e)
        {
            if(MainWindow.connected)
            {
                systemInfoChecked = "System";
            }
            else
	            {
                // Initialise Performance Counter
                try
                {
                    Perf_System = GetPerformanceCounterList.ListCounters(new string[] { "System" });

                    dt_system_info.Stop();
                    dt_system_info = new DispatcherTimer();
                    dt_system_info.Interval = TimeSpan.FromSeconds(1);
                    dt_system_info.Tick += SystemInfo;
                    dt_system_info.Start();
                }
                catch (Exception ex)
                {
                    ErrorUSC = new MainWindowError("Error : Problem in loading System Informations", ex.Message, false);
                    EnvValues.Children.Add(ErrorUSC);
                } 
            }
        }

        private void CPUButton_Click(object sender, RoutedEventArgs e)
        {
            if(MainWindow.connected)
            {
                systemInfoChecked = "CPU";
            }
            else
	            {
                // Initialise Performance Counter
                try
                {
                    Perf_System = GetPerformanceCounterList.ListCounters(new string[] { "Processor", "Processor Information" });

                    dt_system_info.Stop();
                    dt_system_info = new DispatcherTimer();
                    dt_system_info.Interval = TimeSpan.FromSeconds(1);
                    dt_system_info.Tick += SystemInfo;
                    dt_system_info.Start();
                }
                catch (Exception ex)
                {
                    ErrorUSC = new MainWindowError("Error : Problem in loading CPU Informations", ex.Message, false);
                    EnvValues.Children.Add(ErrorUSC);
                } 
            }
        }

        private void NetworkButton_Click(object sender, RoutedEventArgs e)
        {
            if(MainWindow.connected)
            {
                systemInfoChecked = "Network";
            }
            else
	        {
                // Initialise Performance Counter
                try
                {
                    Perf_System = GetPerformanceCounterList.ListCounters(new string[] { "Network Adapter", "Network Interface" });

                    dt_system_info.Stop();
                    dt_system_info = new DispatcherTimer();
                    dt_system_info.Interval = TimeSpan.FromSeconds(1);
                    dt_system_info.Tick += SystemInfo;
                    dt_system_info.Start();
                }
                catch (Exception ex)
                {
                    ErrorUSC = new MainWindowError("Error : Problem in loading Network Informations", ex.Message, false);
                    EnvValues.Children.Add(ErrorUSC);
                } 
            }
        }

        private void OSButton_Click(object sender, RoutedEventArgs e)
        {
            if(MainWindow.connected)
            {
                systemInfoChecked = "OS";
            }
            else
	        {
                try
                {
                    dt_system_info.Stop();

                    OperatingSystem os = Environment.OSVersion;
                    Version ver = os.Version;
                    ObservableCollection<Info> systemInfo = new ObservableCollection<Info>{
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

                    if (systemInfo.Count > 5)
                    {
                        int split = Convert.ToInt32(systemInfo.Count / 2);
                        ObservableCollection<Info> split1 = new ObservableCollection<Info>();
                        ObservableCollection<Info> split2 = new ObservableCollection<Info>();

                        for (int i = 0; i < split; i++)
                        {
                            split1.Add(systemInfo[i]);
                        }

                        for (int i = split; i < systemInfo.Count; i++)
                        {
                            split2.Add(systemInfo[i]);
                        }

                        GridValue1.ItemsSource = split1;
                        GridValue2.ItemsSource = split2;
                    }
                    else
                        GridValue1.ItemsSource = systemInfo;
                }
                catch (Exception ex)
                {
                    ErrorUSC = new MainWindowError("Error : Problem in loading OS Informations", ex.Message, false);
                    EnvValues.Children.Add(ErrorUSC);
                } 
            }
        }

        private void MemoryButton_Click(object sender, RoutedEventArgs e)
        {
            if(MainWindow.connected)
            {
                systemInfoChecked = "Memory";
            }
            else
	        {
                // Initialise Performance Counter
                try
                {
                    Perf_System = GetPerformanceCounterList.ListCounters(new string[] { "Memory" });

                    dt_system_info.Stop();
                    dt_system_info = new DispatcherTimer();
                    dt_system_info.Interval = TimeSpan.FromSeconds(1);
                    dt_system_info.Tick += SystemInfo;
                    dt_system_info.Start();
                }
                catch (Exception ex)
                {
                    ErrorUSC = new MainWindowError("Error : Problem in loading Memory Informations", ex.Message, false);
                    EnvValues.Children.Add(ErrorUSC);
                } 
            }
        }

        private void ComboExe_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

            try
            {
                if (ComboExe.Items.Count>0)
                {
                    GridDll.ItemsSource = GetDll(ComboExe.SelectedValue.ToString());

                    if (dt_graph != null)
                    {
                        try
                        {
                            dt_graph.Stop();
                            total_cpu = new PerformanceCounter("Process", "% Processor Time", "_Total");
                            process_cpu = new PerformanceCounter("Process", "% Processor Time", ComboExe.SelectedValue.ToString());
                            IOData = new PerformanceCounter("Process", "IO Data Bytes/sec", ComboExe.SelectedValue.ToString());
                            WSPCounter = new PerformanceCounter("Process", "Working Set - Private", ComboExe.SelectedValue.ToString());
                            dt_graph = new DispatcherTimer();
                            dt_graph.Interval = TimeSpan.FromSeconds(1);
                            dt_graph.Tick += ChangeGraph;
                            dt_graph.Start();
                        }
                        catch (Exception ex)
                        {
                            if (ComboExe.Items.Count > 0)
                            {
                                ComboExe.SelectedIndex = 0;
                            }
                            else
                            {
                                ErrorUSC = new MainWindowError("Error : Problem in loading Graphs", ex.Message, false);
                                Graph.Children.Clear();
                                Graph.Children.Add(ErrorUSC);

                                ErrorUSC = new MainWindowError("Error : Problem in loading dlls", ex.Message, false);
                                Dll.Children.Add(ErrorUSC);
                            }
                        }
                    }
                    else
                    {
                        #region Get UiPath Processes
                        Process[] processlist = Process.GetProcesses();
                        foreach (Process theprocess in processlist)
                        {
                            if (theprocess.ProcessName.Contains("UiPath"))
                            {
                                string fileName = theprocess.MainModule.FileName;
                                FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(fileName);
                                string company = fvi.CompanyName;
                                if (company.Contains("UiPath") && !ComboExe.Items.Contains(theprocess.ProcessName))
                                    ComboExe.Items.Add(theprocess.ProcessName);
                            }
                        }
                        if (ComboExe.Items.Count > 0)
                        {
                            ComboExe.SelectedIndex = 0;
                        }
                        #endregion
                    }
                }
            }
            catch (Exception ex)
            {
                if (ComboExe.Items.Count == 0)
                {
                    ErrorUSC = new MainWindowError("Error : Problem in loading dlls", ex.Message, false);
                }                   
                else
                    ErrorUSC = new MainWindowError("Error : Problem in loading " + ComboExe.SelectedValue.ToString() + " dlls", ex.Message, false);
                Dll.Children.Clear();
                Dll.Children.Add(ErrorUSC);
            }
        }

        private void lost_focus(object sender, RoutedEventArgs e)
        {
            if (ErrorUSC != null)
            {
                //Dll.Children.Remove(ErrorUSC);
                DashboardUserControl.Children.Remove(ErrorUSC);
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            UserControl settings = new Settings.Settings();
            DashboardUserControl.Children.Add(settings);

        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            dt_connectionMontor.Stop();
            dt_exeMonitor.Stop();
        }
    }

    public class Info
    {
        public String Property { get; set; }
        public String Value { get; set; }
    }

    public class DLLInfo
    {
        public String Name { get; set; }
        public String Description { get; set; }
        public String Company { get; set; }
        public String Path { get; set; }
    }
}
