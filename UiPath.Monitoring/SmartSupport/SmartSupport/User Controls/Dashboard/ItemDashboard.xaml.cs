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
using System.Threading;

namespace SmartSupport.User_Controls.Dashboard
{
    /// <summary>
    /// Interaction logic for DashBoard.xaml
    /// </summary>
    public partial class ItemDashboard : UserControl
    {
        private static ObservableCollection<Info> systemInfo = new ObservableCollection<Info>();
        DispatcherTimer dt_system_info;
        DispatcherTimer dt_graph;
        static List<PerformanceCounter> Perf_System;
        PerformanceCounter total_cpu;
        PerformanceCounter process_cpu;
        PerformanceCounter IOData;
        PerformanceCounter WSPCounter;
        UserControl ErrorUSC = null;
        bool graphErrorFlag = false;
        bool dllErrorFlag = false;
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

            #region Get UiPath Processes
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

        private void DashboardUserControl_Loaded(object sender, RoutedEventArgs e)
        {
            // Load Dlls
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
                Dll.Children.Add(ErrorUSC);
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

            try
            {
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
                ErrorUSC = new MainWindowError("Error : Problem in loading Graphs", ex.Message, false);
                Graph.Children.Add(ErrorUSC);
            }
        }

        private static ObservableCollection<DLLInfo> GetDll(string exe)
        {
            Process toMonitor = Process.GetProcessesByName(exe).FirstOrDefault();

            ObservableCollection<DLLInfo> dlls = new ObservableCollection<DLLInfo>();

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
            if (Perf_System == null)
                return null;
            else
            {
                ObservableCollection<Info> systemInfo = new ObservableCollection<Info>();
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
            try
            {

                float t = total_cpu.NextValue();
                float p = process_cpu.NextValue();
                if (t != 0)
                {
                    double temp = Math.Round(p / t * 100, 2);
                    gauge_cpu.Value = temp;
                    chip_cpu.Content = temp;
                }

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

                WindowState ws = Window.GetWindow(this).WindowState;
                if (ws == WindowState.Minimized)
                {
                    Dll.Children.Clear();
                    Graph.Children.Clear();
                    var temp = Dll.Children;
                }
                else
                {

                    ErrorUSC = new MainWindowError("Error : Problem in loading Graphs", ex.Message, false);
                    //var temp = Graph.Children.OfType<UserControl>().FirstOrDefault();
                    if (!graphErrorFlag || Graph.Children.Count == 0)
                    {
                        graphErrorFlag = true;
                        Graph.Children.Clear();
                        Graph.Children.Add(ErrorUSC);
                    }

                    if (!dllErrorFlag || Dll.Children.Count == 0)
                    {
                        dllErrorFlag = true;
                        UserControl tempUC = new MainWindowError("Error : Problem in loading dlls", ex.Message, false);
                        //ErrorUSC = new MainWindowError("Error : Problem in loading dlls", ex.Message, false);
                        Dll.Children.Add(tempUC);
                    }
                }
            }

        }

        private void SystemButton_Click(object sender, RoutedEventArgs e)
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

        private void CPUButton_Click(object sender, RoutedEventArgs e)
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

        private void NetworkButton_Click(object sender, RoutedEventArgs e)
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

        private void OSButton_Click(object sender, RoutedEventArgs e)
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

        private void MemoryButton_Click(object sender, RoutedEventArgs e)
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

        private void ComboExe_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

            try
            {
                if (ComboExe.Items.Count > 0)
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
