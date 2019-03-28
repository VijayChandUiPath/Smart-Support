using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Diagnostics;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Controls.Primitives;
using Microsoft.Win32;
using System.IO;
using SmartSupport.User_Controls.Error;
using System.Threading;
using System.Configuration;
using Microsoft.AspNet.SignalR.Client;
using Newtonsoft.Json;

namespace SmartSupport.User_Controls.Logging
{
    /// <summary>
    /// Interaction logic for Logs_Reg_GP_Trac.xaml
    /// </summary>
   // public partial class Logs_Reg_GP_Trac : UserControl
    // public partial class UserControlCreate : UserControl
    public partial class Logging : UserControl
    {

        static int evtLogCount;
        static IEnumerable<List<string>> logs = new List<List<string>>();
        DataTable dtlogs, dtOriginalLogs;
        UserControl ErrorUSC = null;
        List<DDL_Level> objLevelList;
        public List<string> filteredLevels = new List<string>();
        List<DDL_Source> objSourceList;
        public List<string> filteredSource = new List<string>();
        private ListView lvPriviledges;
        private ProgressBar.ProgressBarWindow pbw = new ProgressBar.ProgressBarWindow();
        public delegate void OnWorkerMethodCompleteDelegate();
        public event OnWorkerMethodCompleteDelegate OnWorkerComplete;

        /// <summary>
        /// Provides access to the LSA functions
        /// </summary>
        LsaWrapper LSA;

        public Logging()
        {
            InitializeComponent();

            if (logs.Count() == 0)
            {

                try
                {
                    ThreadStart tStart = new ThreadStart(WorkerMethod);
                    Thread t = new Thread(tStart);
                    t.Start();

                    pbw.ShowDialog();

                }
                catch (Exception ex)
                {
                    ErrorUSC = new MainWindowError("Error : Problem in loading Event Viewer Logs", ex.Message, false);
                    EventViewerGridMain.Children.Add(ErrorUSC);
                }
            }

            BindEVTTable();
            objLevelList = new List<DDL_Level>();
            AddElementsInLevelList();
            BindLevelDropDown();

            objSourceList = new List<DDL_Source>();
            AddElementsInSourceList();
            BindSourceDropDown();
        }


        public void WorkerMethod()
        {
            EVTLogDetails("GetEVTApplicationLogs");

            pbw.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal,
            new Action(
                delegate ()
                {
                    pbw.Close();
                }
            ));
        }

        public static IEnumerable<List<string>> GetEVTApplicationLogs()
        {
            if (MainWindow.connected)
            {
                var ls = new List<List<string>>();
                MainWindow._hub.On("ReceiveEVTAppLogData", x => {
                    File.AppendAllText(@"log.txt", x + Environment.NewLine);
                    ls = JsonConvert.DeserializeObject<List<List<string>>>(x);
                });

                MainWindow._hub.Invoke("Server", "EVTAppLog").Wait();
                MainWindow.command = "EVTAppLog";
                evtLogCount = ls[0].Count();
                return ls;

            }
            else
            {
                string eventLogName = "Application";
                string sourceName = "UiPath";
                string machineName = System.Environment.MachineName;

                EventLog eventLog = new EventLog();
                eventLog.Log = eventLogName;
                eventLog.Source = sourceName;
                eventLog.MachineName = machineName;

                //var result = (from EventLogEntry elog in eventLog.Entries
                //              where (elog.Source.ToString().Equals("UiPath"))
                //              orderby elog.TimeGenerated descending
                //              where (elog.TimeGenerated >= DateTime.Now.AddDays(-30))
                //select elog).ToList();

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
                    // Console.WriteLine();
                    // Console.WriteLine(result[i].Message);
                    EVTMSGDetails.Add(result[i].Message);
                    EVTSRCDetails.Add(result[i].Source);
                    EVTTimeDetails.Add(Convert.ToString(result[i].TimeGenerated));
                    EVTEntryType.Add(Convert.ToString(result[i].EntryType));
                }

                //Console.WriteLine(result.Count);
                return new List<List<string>> { EVTSRCDetails, EVTTimeDetails, EVTEntryType, EVTMSGDetails };
            }
        }

        public static IEnumerable<List<string>> GetEVTSecurityLogs()
        {
            //string eventLogName = "Security";
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

            //Console.WriteLine(result.Count);
            return new List<List<string>> { EVTSRCDetails, EVTTimeDetails, EVTEntryType, EVTMSGDetails };

        }

        public void EVTLogDetails(string methodName)
        {
            try
            {
                if (methodName == "GetEVTApplicationLogs")
                {
                    logs = GetEVTApplicationLogs();

                }
                else if (methodName == "GetEVTSecurityLogs")
                {
                    logs = GetEVTSecurityLogs();
                }
            }

            catch (Exception ex)
            {
                ErrorUSC = new MainWindowError("Error : Problem in loading Event Viewer Logs", ex.Message, false);
                EventViewerGridMain.Children.Add(ErrorUSC);

            }


        }

        public void BindEVTTable()
        {
            dtlogs = new DataTable();
            dtlogs.Columns.Add(new DataColumn("Source", Type.GetType("System.String")));
            dtlogs.Columns.Add(new DataColumn("DateandTime", Type.GetType("System.String")));
            dtlogs.Columns.Add(new DataColumn("Level", Type.GetType("System.String")));
            dtlogs.Columns.Add(new DataColumn("Message", Type.GetType("System.String")));
            //dt.Columns.Add("PATH");

            //EventViewerLogs logsI = new EventViewerLogs();

            try
            {
                for (int i = 0; i < evtLogCount; i++)
                {
                    dtlogs.Rows.Add(logs.ElementAt(0)[i], logs.ElementAt(1)[i], logs.ElementAt(2)[i], logs.ElementAt(3)[i]);

                }


                if (dtlogs.Rows.Count > 0)
                {
                    dtOriginalLogs = dtlogs;

                    EVtGrid.ItemsSource = dtlogs.DefaultView;
                    EVtGrid.SelectedIndex = 0;


                    //txtEVTDetail.Document.Blocks.Add(new Paragraph(new Run(logs.ElementAt(3)[0])));

                    txtEVTDetail.Document.Blocks.Clear();
                    txtEVTDetail.Document.Blocks.Add(new Paragraph(new Run((dtlogs.Rows[0][3]).ToString())));
                }
            }
            catch (Exception ex)
            {
                ErrorUSC = new MainWindowError("Error : Problem in loading Event Viewer Logs", ex.Message, false);
                EventViewerGridMain.Children.Add(ErrorUSC);

            }
        }


        private void EVtGrid_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            try
            {
                //txtEVTDetail.Document.Blocks.Clear();
                DependencyObject dep = (DependencyObject)e.OriginalSource;

                // iteratively traverse the visual tree
                while ((dep != null) && !(dep is DataGridCell) && !(dep is DataGridColumnHeader) && !(dep is System.Windows.Documents.Paragraph) && !(dep is System.Windows.Documents.FlowDocument))
                {
                    dep = VisualTreeHelper.GetParent(dep);
                }

                if (dep == null)
                    return;

                if (dep is DataGridColumnHeader)
                {
                    DataGridColumnHeader columnHeader = dep as DataGridColumnHeader;
                    // do something
                }

                if (dep is DataGridCell)
                {
                    DataGridCell cell = dep as DataGridCell;

                    while ((dep != null) && !(dep is DataGridRow))
                    {
                        dep = VisualTreeHelper.GetParent(dep);
                    }

                    DataGridRow row = dep as DataGridRow;
                    int k = FindRowIndex(row);
                    //txtEVTDetail.Document.Blocks.Add(new Paragraph(new Run(logs.ElementAt(3)[k])));
                    if (dtlogs.Rows.Count != 0)
                    {
                        txtEVTDetail.Document.Blocks.Clear();
                        txtEVTDetail.Document.Blocks.Add(new Paragraph(new Run((dtlogs.Rows[k][3]).ToString())));
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorUSC = new MainWindowError("Error : Problem in loading Event Viewer Logs Details", ex.Message, false);
                EVTLogDetailsGrid.Children.Add(ErrorUSC);

            }

        }

        private int FindRowIndex(DataGridRow row)
        {
            DataGrid dataGrid =
                ItemsControl.ItemsControlFromItemContainer(row)
                as DataGrid;

            int index = dataGrid.ItemContainerGenerator.
                IndexFromContainer(row);

            return index;
        }

        private void btnSearch_click(object sender, RoutedEventArgs e)
        {
            try
            {
                TextRange text = new TextRange(tbSearch.Document.ContentStart, tbSearch.Document.ContentEnd);

                string str = text.Text.Trim();
                String[] splited = str.Split(' ');

                bool contains = dtlogs.AsEnumerable().Any(row => splited.Any(row.Field<String>("Message").ToLower().Contains));
                if (contains)
                {
                    DataTable tblFiltered = dtlogs.AsEnumerable()
                    .Where(row => splited.Any(row.Field<String>("Message").ToLower().Contains))
                    .CopyToDataTable();
                    // EVtGrid.ItemsSource = tblFiltered.DefaultView;

                    // tblFiltered = tblFiltered.DefaultView.ToTable();

                    EVtGrid.ItemsSource = tblFiltered.DefaultView;
                    EVtGrid.SelectedIndex = 0;
                    dtlogs = tblFiltered;
                    txtEVTDetail.Document.Blocks.Clear();
                    //txtEVTDetail.Document.Blocks.Add(new Paragraph(new Run(logs.ElementAt(3)[0])));
                    if (dtlogs.Rows.Count != 0)
                    {
                        txtEVTDetail.Document.Blocks.Add(new Paragraph(new Run((dtlogs.Rows[0][3]).ToString())));
                    }
                }
                else
                {
                    EVtGrid.ItemsSource = "";// dtlogs.DefaultView;
                    txtEVTDetail.Document.Blocks.Clear();
                }

            }

            catch (Exception ex)
            {
                ErrorUSC = new MainWindowError("Error : Problem in loading Event Viewer Logs", ex.Message, false);
                EventViewerGridMain.Children.Add(ErrorUSC);

            }

        }

        //private void btnSecAppLogs_click(object sender, RoutedEventArgs e)
        //{
        //    if (btnSecAppLogs.Content.ToString() == "View Security Logs")
        //    {
        //        EVTLogDetails("GetEVTSecurityLogs");
        //        btnSecAppLogs.Content = "View Application Logs";
        //    }
        //    else if (btnSecAppLogs.Content.ToString() == "View Application Logs")
        //    {
        //        EVTLogDetails("GetEVTApplicationLogs");
        //        btnSecAppLogs.Content = "View Security Logs";
        //    }

        //}


        private void BindLevelDropDown()
        {
            ddlLevel.ItemsSource = objLevelList;
        }

        private void BindSourceDropDown()
        {
            ddlSource.ItemsSource = objSourceList;
        }


        private void ddlLevel_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }


        private void ddlSource_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        //private void ddlLevel_TextChanged(object sender, TextChangedEventArgs e)
        //{
        //    ddlLevel.ItemsSource = objLevelList.Where(x => x.Level_Name.StartsWith(ddlLevel.Text.Trim()));
        //}

        private void AllCheckbocx_CheckedAndUncheckedLevel(object sender, RoutedEventArgs e)
        {
            BindLevelListBOX();
        }

        private void AllCheckbocx_CheckedAndUncheckedSource(object sender, RoutedEventArgs e)
        {
            BindSourceListBOX();

        }

        private void BindLevelListBOX()
        {
            try
            {
                filteredLevels.Clear();
                tbSearch.Document.Blocks.Clear();
                DataTable dtFilteredLogs = dtOriginalLogs;
                string strFilteredSource = "'UiPath','UiRobotSvc','.NET Runtime','Application Error'";
                string strFilteredLevels = "'Critical','Warning','Verbose','Error','Information'";
                //testListbox.Items.Clear();
                foreach (var level in objLevelList)
                {
                    if (level.Check_LevelStatus == true)
                    {
                        filteredLevels.Add(level.Level_Name);

                    }
                }

                if (dtFilteredLogs != null)
                {
                    if (filteredSource.Count != 0 && filteredLevels.Count == 0)
                    {
                        strFilteredSource = "'" + String.Join("','", filteredSource) + "'";
                        dtFilteredLogs.DefaultView.RowFilter = "Source IN (" + strFilteredSource + ")";
                    }
                    else if (filteredSource.Count == 0 && filteredLevels.Count != 0)
                    {
                        strFilteredLevels = "'" + String.Join("','", filteredLevels) + "'";
                        dtFilteredLogs.DefaultView.RowFilter = "Level IN (" + strFilteredLevels + ")";
                    }
                    else if (filteredSource.Count != 0 && filteredLevels.Count != 0)
                    {
                        strFilteredLevels = "'" + String.Join("','", filteredLevels) + "'";
                        strFilteredSource = "'" + String.Join("','", filteredSource) + "'";
                        dtFilteredLogs.DefaultView.RowFilter = "Source IN (" + strFilteredSource + ") AND Level IN (" + strFilteredLevels + ")";
                    }
                    else
                    {
                        dtFilteredLogs.DefaultView.RowFilter = "Source IN (" + strFilteredSource + ") AND Level IN (" + strFilteredLevels + ")";
                    }


                    dtFilteredLogs = dtFilteredLogs.DefaultView.ToTable();

                    EVtGrid.ItemsSource = dtFilteredLogs.DefaultView;
                    EVtGrid.SelectedIndex = 0;
                    dtlogs = dtFilteredLogs;
                    txtEVTDetail.Document.Blocks.Clear();
                    //txtEVTDetail.Document.Blocks.Add(new Paragraph(new Run(logs.ElementAt(3)[0])));
                    if (dtlogs.Rows.Count != 0)
                    {
                        txtEVTDetail.Document.Blocks.Add(new Paragraph(new Run((dtlogs.Rows[0][3]).ToString())));
                    }
                }
            }


            catch (Exception ex)
            {
                ErrorUSC = new MainWindowError("Error : Problem in loading Event Viewer Logs", ex.Message, false);
                EventViewerGridMain.Children.Add(ErrorUSC);

            }

        }

        private void BindSourceListBOX()
        {
            try
            {
                filteredSource.Clear();
                tbSearch.Document.Blocks.Clear();
                DataTable dtFilteredLogs = dtOriginalLogs;
                string strFilteredSource = "'UiPath','UiRobotSvc','.NET Runtime','Application Error'";
                string strFilteredLevels = "'Critical','Warning','Verbose','Error','Information'";
                //testListbox.Items.Clear();
                foreach (var source in objSourceList)
                {
                    if (source.Check_SourceStatus == true)
                    {
                        filteredSource.Add(source.Source_Name);

                    }
                }

                if (dtFilteredLogs != null)
                {
                    if (filteredSource.Count != 0 && filteredLevels.Count == 0)
                    {
                        strFilteredSource = "'" + String.Join("','", filteredSource) + "'";
                        dtFilteredLogs.DefaultView.RowFilter = "Source IN (" + strFilteredSource + ")";
                    }
                    else if (filteredSource.Count == 0 && filteredLevels.Count != 0)
                    {
                        strFilteredLevels = "'" + String.Join("','", filteredLevels) + "'";
                        dtFilteredLogs.DefaultView.RowFilter = "Level IN (" + strFilteredLevels + ")";
                    }
                    else if (filteredSource.Count != 0 && filteredLevels.Count != 0)
                    {
                        strFilteredLevels = "'" + String.Join("','", filteredLevels) + "'";
                        strFilteredSource = "'" + String.Join("','", filteredSource) + "'";
                        dtFilteredLogs.DefaultView.RowFilter = "Source IN (" + strFilteredSource + ") AND Level IN (" + strFilteredLevels + ")";
                    }
                    else
                    {
                        dtFilteredLogs.DefaultView.RowFilter = "Source IN (" + strFilteredSource + ") AND Level IN (" + strFilteredLevels + ")";
                    }
                    dtFilteredLogs = dtFilteredLogs.DefaultView.ToTable();

                    EVtGrid.ItemsSource = dtFilteredLogs.DefaultView;
                    EVtGrid.SelectedIndex = 0;
                    dtlogs = dtFilteredLogs;
                    txtEVTDetail.Document.Blocks.Clear();
                    //txtEVTDetail.Document.Blocks.Add(new Paragraph(new Run(logs.ElementAt(3)[0])));
                    if (dtlogs.Rows.Count != 0)
                    {
                        txtEVTDetail.Document.Blocks.Add(new Paragraph(new Run((dtlogs.Rows[0][3]).ToString())));
                    }
                }
            }


            catch (Exception ex)
            {
                ErrorUSC = new MainWindowError("Error : Problem in loading Event Viewer Logs", ex.Message, false);
                EventViewerGridMain.Children.Add(ErrorUSC);

            }
        }

        private void AddElementsInLevelList()
        {
            // 1 element  
            DDL_Level obj = new DDL_Level();
            obj.Level_Name = "Critical";
            objLevelList.Add(obj);
            obj = new DDL_Level();
            obj.Level_Name = "Warning";
            objLevelList.Add(obj);
            obj = new DDL_Level();
            obj.Level_Name = "Verbose";
            objLevelList.Add(obj);
            obj = new DDL_Level();
            obj.Level_Name = "Error";
            objLevelList.Add(obj);
            obj = new DDL_Level();
            obj.Level_Name = "Information";
            objLevelList.Add(obj);
        }


        private void ETLButton_Click(object sender, RoutedEventArgs e)
        {
            if (ErrorUSC != null)
            {
                RemoveErrorWindow();
            }
            EventViewerGridMain.Visibility = Visibility.Hidden;
            lblLogDetails.Visibility = Visibility.Hidden;
            EVTLogDetailsGrid.Visibility = Visibility.Hidden;
            ETLGrid.Visibility = Visibility.Visible;
            GPGrid.Visibility = Visibility.Hidden;
            RegGrid.Visibility = Visibility.Hidden;
        }

        private void EVTButton_Click(object sender, RoutedEventArgs e)
        {
            if (ErrorUSC != null)
            {
                RemoveErrorWindow();
            }
            EventViewerGridMain.Visibility = Visibility.Visible;
            lblLogDetails.Visibility = Visibility.Visible;
            EVTLogDetailsGrid.Visibility = Visibility.Visible;
            ETLGrid.Visibility = Visibility.Hidden;
            GPGrid.Visibility = Visibility.Hidden;
            RegGrid.Visibility = Visibility.Hidden;
        }

        private void btnEnableTracing_click(object sender, RoutedEventArgs e)
        {
            KBGrid.Visibility = Visibility.Hidden;
            string path = @"C:\Program Files (x86)\UiPath\Studio";
            if (Directory.Exists(path))
            {
                string command = "UiRobot.exe --enableLowLevel";
                GenerateCMDLogs(command);
                btnDisableTracing.IsEnabled = true;
                btnEnableTracing.IsEnabled = false;
                txtETLTips.Document.Blocks.Clear();
                txtETLTips.Document.Blocks.Add(new Paragraph(new Run("Service and executor verbose tracing enabled." + Environment.NewLine + "Low level driver tracing started." + Environment.NewLine + "Kindly perform the operation..." + Environment.NewLine + Environment.NewLine + Environment.NewLine + "Note: Do not Forget to Disable Tracing once the operation is completed...")));

            }
            else
            {
                ErrorUSC = new MainWindowError("Error : Make sure UiPath is installed!", "Make sure UiPath is installed!", true);
                ETLDiagGrid.Children.Add(ErrorUSC);
            }


        }

        private void btnDisableTracing_click(object sender, RoutedEventArgs e)
        {
            KBGrid.Visibility = Visibility.Hidden;
            string path = @"C:\Program Files (x86)\UiPath\Studio";
            if (Directory.Exists(path))
            {
                string command = "UiRobot.exe --disableLowLevel";
                GenerateCMDLogs(command);
                btnDisableTracing.IsEnabled = false;
                btnEnableTracing.IsEnabled = true;
                txtETLTips.Document.Blocks.Clear();
                txtETLTips.Document.Blocks.Add(new Paragraph(new Run("Service and executor verbose tracing disabled." + Environment.NewLine + "Low level driver tracing stopped." + Environment.NewLine + Environment.NewLine + "UiPathTrace-xxxxxxxx.etl file was generated on Desktop.")));
            }
            else
            {
                ErrorUSC = new MainWindowError("Error : Make sure UiPath is installed!", "Make sure UiPath is installed!", true);
                ETLDiagGrid.Children.Add(ErrorUSC);
            }
        }

        public void GenerateCMDLogs(string command)
        {
            try
            {
                var proc1 = new ProcessStartInfo();
                // string anyCommand;
                proc1.UseShellExecute = true;

                proc1.WorkingDirectory = @"C:\Program Files (x86)\UiPath\Studio";

                proc1.FileName = "cmd.exe";
                proc1.Verb = "runas";
                proc1.Arguments = "/c " + command;
                proc1.WindowStyle = ProcessWindowStyle.Hidden;
                Process.Start(proc1);
            }
            catch (Exception ex)
            {
                ErrorUSC = new MainWindowError("Error : Make sure UiPath is installed!", ex.Message, false);
                ETLDiagGrid.Children.Add(ErrorUSC);

            }
        }

        private void GPEdit_Click(object sender, RoutedEventArgs e)
        {
            if (ErrorUSC != null)
            {
                RemoveErrorWindow();
            }
            GPGrid.Visibility = Visibility.Visible;
            EventViewerGridMain.Visibility = Visibility.Hidden;
            lblLogDetails.Visibility = Visibility.Hidden;
            EVTLogDetailsGrid.Visibility = Visibility.Hidden;
            ETLGrid.Visibility = Visibility.Hidden;
            RegGrid.Visibility = Visibility.Hidden;

            ReadPrivileges();
        }


        private void RemoveErrorWindow()
        {
            //ETLDiagGrid.Children.Remove(ErrorUSC);
            //EventViewerGridMain.Children.Remove(ErrorUSC);
            //ETLDiagGrid.Children.Remove(ErrorUSC);
            //EVTLogDetailsGrid.Children.Remove(ErrorUSC);
            //GPGrid.Children.Remove(ErrorUSC);
            //RegGrid.Children.Remove(ErrorUSC);
            EventViewerGridMain.Children.Remove(ErrorUSC);
            ETLDiagGrid.Children.Remove(ErrorUSC);
            EVTLogDetailsGrid.Children.Remove(ErrorUSC);
            GPGrid.Children.Remove(ErrorUSC);
            Parent_RegGrid.Children.Remove(ErrorUSC);
        }

        private void AddElementsInSourceList()
        {
            // 1 element  
            DDL_Source obj = new DDL_Source();
            obj.Source_Name = "UiPath";
            objSourceList.Add(obj);
            obj = new DDL_Source();
            obj.Source_Name = "UiRobotSvc";
            objSourceList.Add(obj);
            obj = new DDL_Source();
            obj.Source_Name = ".NET Runtime";
            objSourceList.Add(obj);
            obj = new DDL_Source();
            obj.Source_Name = "Application Error";
            objSourceList.Add(obj);
        }


        private void ReadPrivileges()
        {
            try
            {
                DataTable dtGP = new DataTable();
                dtGP.Columns.Add(new DataColumn("Policy", Type.GetType("System.String")));
                dtGP.Columns.Add(new DataColumn("SecuritySetting", Type.GetType("System.String")));
                //lvPriviledges.Items.Clear();
                LSA = new LsaWrapper("localhost");
                ReadPrivilege("SeBatchLogonRight", dtGP);
                ReadPrivilege("SeDenyBatchLogonRight", dtGP);
                ReadPrivilege("SeDenyInteractiveLogonRight", dtGP);
                ReadPrivilege("SeDenyNetworkLogonRight", dtGP);
                ReadPrivilege("SeDenyRemoteInteractiveLogonRight", dtGP);
                ReadPrivilege("SeDenyServiceLogonRight", dtGP);
                ReadPrivilege("SeInteractiveLogonRight", dtGP);
                ReadPrivilege("SeNetworkLogonRight", dtGP);
                ReadPrivilege("SeRemoteInteractiveLogonRight", dtGP);
                GPDataGrid.ItemsSource = dtGP.DefaultView;
                LSA.Dispose();

            }
            catch (Exception ex)
            {
                ErrorUSC = new MainWindowError("Error : Problem in loading Group Policies", ex.Message, false);
                GPGrid.Children.Add(ErrorUSC);

            }
        }


        /// <summary>
        /// Reads the privilege with the specified name and displays the users with this privilege to the listview
        /// </summary>
        /// <param name="PrivilegeName">The name of the privilege - for example "SeTimeZonePrivilege"</param>
        private void ReadPrivilege(String PrivilegeName, DataTable dtGP)
        {
            string policyName = "";
            switch (PrivilegeName)
            {
                case "SeBatchLogonRight":
                    policyName = "Log on as a batch job";
                    break;
                case "SeDenyBatchLogonRight":
                    policyName = "Deny Log on as a batch job";
                    break;
                case "SeDenyInteractiveLogonRight":
                    policyName = "Deny log on locally";
                    break;
                case "SeDenyNetworkLogonRight":
                    policyName = "Deny access to this computer from the network";
                    break;
                case "SeDenyRemoteInteractiveLogonRight":
                    policyName = "Deny log on through Remote Desktop Services";
                    break;
                case "SeDenyServiceLogonRight":
                    policyName = "Deny log on as a service";
                    break;
                case "SeInteractiveLogonRight":
                    policyName = "Allow Log on locally";
                    break;
                case "SeNetworkLogonRight":
                    policyName = "Access to this computer from the network";
                    break;
                case "SeRemoteInteractiveLogonRight":
                    policyName = "Allow log on through Remote Desktop Services";
                    break;

            }


            //ListViewGroup PrivilegeGroup = lvPriviledges.Groups.Add(PrivilegeName, GetString(PrivilegeName));




            string account = "";

            List<String> Accounts = LSA.ReadPrivilege(PrivilegeName);
            foreach (string val in Accounts)
            {
                account = account + val + ", ";
            }
            if (!(account == null) && account != "")
            {
                account = account.Substring(0, account.LastIndexOf(','));
            }
            if (account == "")
            {
                account = "--";
            }
            dtGP.Rows.Add(policyName, account);

        }

        private void btnGPEdit_click(object sender, RoutedEventArgs e)
        {
            var proc1 = new ProcessStartInfo();
            // string anyCommand;
            proc1.UseShellExecute = true;

            //proc1.WorkingDirectory = @"C:\Program Files (x86)\UiPath\Studio";

            proc1.FileName = "cmd.exe";
            proc1.Verb = "runas";
            proc1.Arguments = "/c " + "gpedit.msc";
            proc1.WindowStyle = ProcessWindowStyle.Hidden;
            Process.Start(proc1);
        }
        #region Registry Key
        private void Registry_Click(object sender, RoutedEventArgs e)
        {
            if (ErrorUSC != null)
            {
                RemoveErrorWindow();
            }
            RegGrid.Visibility = Visibility.Visible;
            GPGrid.Visibility = Visibility.Hidden;
            EventViewerGridMain.Visibility = Visibility.Hidden;
            lblLogDetails.Visibility = Visibility.Hidden;
            EVTLogDetailsGrid.Visibility = Visibility.Hidden;
            ETLGrid.Visibility = Visibility.Hidden;

            ReadKey();
        }

        int Regcount = 0;
        string studioFolderPath;
        string fileName;
        private void ReadKey()
        {
            #region Get Registry Key
            RegistryKey rk = Registry.CurrentUser.OpenSubKey("Software").OpenSubKey("Microsoft").OpenSubKey("Windows").OpenSubKey("CurrentVersion").OpenSubKey("Internet Settings").OpenSubKey("Zones").OpenSubKey("3");
            PrintKeys(rk, "");
            #endregion

            RegItem.SelectedIndex = 0;

            title.Text = ((System.Windows.Controls.ContentControl)RegItem.SelectedItem).Content.ToString();

        }

        void PrintKeys(RegistryKey rkey, string defaultValue)
        {
            try
            {
                String[] names = new string[0];
                string[] abc = new string[0];

                // Retrieve all the subkeys for the specified key.
                names = rkey.GetSubKeyNames();


                abc = rkey.GetValueNames();

                DataTable dtReg = new DataTable();
                dtReg.Columns.Add(new DataColumn("Name", Type.GetType("System.String")));
                dtReg.Columns.Add(new DataColumn("Data", Type.GetType("System.String")));


                List<string> keysDetail = new List<string>();
                // Print the contents of the array to the console.
                foreach (String s in abc)
                {
                    if (!(s == ""))
                    {
                        if (rkey.GetValue(s).ToString() == "System.Byte[]")
                        {
                            dtReg.Rows.Add(s, BitConverter.ToString(rkey.GetValue(s) as byte[]));
                        }
                        else
                            dtReg.Rows.Add(s, rkey.GetValue(s).ToString());
                    }

                    else if (defaultValue == "")
                    {
                        //dtReg.Rows.Add();
                    }
                    else
                    {
                        dtReg.Rows.Add("(Default)", defaultValue);
                    }

                }
                Regvalue.ItemsSource = dtReg.DefaultView;

            }

            catch (Exception ex)
            {
                ErrorUSC = new MainWindowError("Error : Application or Relevant key might not be present in the machine", ex.Message, true);
                Parent_RegGrid.Children.Add(ErrorUSC);

            }

        }

        //Checking Registry Component list value
        private void RegItem_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

            RegistryKey rk = null;
            string Office = OfficeVersion();
            string excel = ExcelVersion();
            var defaultValue = "(value not set) or null";

            try
            {
                //string content = ((System.Windows.Controls.Primitives.Selector)sender).SelectedItem.ToString();
                //switch (((System.Windows.Controls.ContentControl)((System.Windows.Controls.Primitives.Selector)sender).SelectedItem).Content)
                switch ((((System.Windows.Controls.ContentControl)(((System.Windows.Controls.Primitives.Selector)sender).SelectedItem)).Content).ToString())
                //switch (content)
                {
                    case "Internet Explorer":
                        rk = Registry.CurrentUser.OpenSubKey("Software").OpenSubKey("Microsoft").OpenSubKey("Windows").OpenSubKey("CurrentVersion").OpenSubKey("Internet Settings").OpenSubKey("Zones").OpenSubKey("3");
                        break;
                    case "Chrome":
                        rk = Registry.CurrentUser.OpenSubKey("Software").OpenSubKey("Google").OpenSubKey("Chrome").OpenSubKey("NativeMessagingHosts").OpenSubKey("com.uipath.chromenativemsg");
                        defaultValue = rk.GetValue(null).ToString();
                        break;
                    //case "Microsoft Office":
                    //    rk = Registry.CurrentUser.OpenSubKey("Software").OpenSubKey("Microsoft").OpenSubKey("Office").OpenSubKey(Office).OpenSubKey("Outlook").OpenSubKey("Security");
                    //    break;
                    case ".Net":
                        rk = Registry.LocalMachine.OpenSubKey("Software").OpenSubKey("Microsoft").OpenSubKey("NET Framework Setup").OpenSubKey("NDP").OpenSubKey("v4").OpenSubKey("Full");
                        break;
                    case "Excel":
                        rk = Registry.ClassesRoot.OpenSubKey("TypeLib").OpenSubKey("{00020813-0000-0000-C000-000000000046}").OpenSubKey(excel);
                        defaultValue = rk.GetValue(null).ToString();
                        break;
                    case "Outlook":
                        rk = Registry.CurrentUser.OpenSubKey("Software").OpenSubKey("Microsoft").OpenSubKey("Office").OpenSubKey(Office).OpenSubKey("Outlook").OpenSubKey("Security");
                        break;
                    case "Terminal Server":
                        rk = Registry.LocalMachine.OpenSubKey("SYSTEM").OpenSubKey("CurrentControlSet").OpenSubKey("Control").OpenSubKey("Terminal Server").OpenSubKey("WinStations").OpenSubKey("RDP-Tcp");
                        break;
                    case "Autologon Password":
                        rk = Registry.LocalMachine.OpenSubKey("Software").OpenSubKey("Microsoft").OpenSubKey("Windows NT").OpenSubKey("CurrentVersion").OpenSubKey("Winlogon");
                        break;
                    case "Uipath":
                        rk = Registry.LocalMachine.OpenSubKey("Software").OpenSubKey("UiPath").OpenSubKey("UiPath Studio");


                        if (rk == null)
                        {
                            rk = Registry.LocalMachine.OpenSubKey("Software").OpenSubKey("UiPath").OpenSubKey("UiPath Desktop");
                        }
                        break;
                    case "Policy Combinations":
                        rk = Registry.LocalMachine.OpenSubKey("SYSTEM").OpenSubKey("CurrentControlSet").OpenSubKey("Control").OpenSubKey("Terminal Server").OpenSubKey("WinStations");
                        break;
                }

                PrintKeys(rk, defaultValue);
                title.Text = ((System.Windows.Controls.ContentControl)RegItem.SelectedItem).Content.ToString();
            }

            catch (Exception ex)
            {
                defaultValue = "";
                rk = Registry.CurrentUser.OpenSubKey("Software");
                //ErrorUSC = new MainWindowError("Error : Registry key might not be present in the machine or Application is installed as an app.", ex.Message, true);
                //Parent_RegGrid.Children.Add(ErrorUSC);
                PrintKeys(rk, defaultValue);
                title.Text = "Unable to fetch Registry details.";
            }

        }
        // Fetching MS Office Version of Robot machine
        public static string OfficeVersion()
        {
            string versionFolder = "";

            //office 2016;
            if (Directory.Exists(@"C:\Program Files (x86)\Microsoft Office\Office16"))
            {
                versionFolder = "16.0";
            }

            //office 2013;
            else if (Directory.Exists(@"C:\Program Files (x86)\Microsoft Office\Office15"))
            {
                versionFolder = "15.0";
            }

            //office 2010;
            else if (Directory.Exists(@"C:\Program Files (x86)\Microsoft Office\Office14"))
            {
                versionFolder = "14.0";
            }
            else
                versionFolder = "16.0";

            return versionFolder;


        }

        // Excel version
        public static string ExcelVersion()
        {
            string ExcelversionFolder = "";

            //office 2016;
            if (Directory.Exists(@"C:\Program Files (x86)\Microsoft Office\Office16"))
            {
                ExcelversionFolder = "1.9";
            }

            //office 2013;
            else if (Directory.Exists(@"C:\Program Files (x86)\Microsoft Office\Office15"))
            {
                ExcelversionFolder = "1.8";
            }

            //office 2010;
            else if (Directory.Exists(@"C:\Program Files (x86)\Microsoft Office\Office14"))
            {
                ExcelversionFolder = "1.7";
            }
            else
                ExcelversionFolder = "1.9";

            return ExcelversionFolder;


        }


        public static string FormatCSV(string input)
        {
            try
            {
                if (input == null)
                    return string.Empty;

                bool containsQuote = false;
                bool containsComma = false;
                int len = input.Length;
                for (int i = 0; i < len && (containsComma == false || containsQuote == false); i++)
                {
                    char ch = input[i];
                    if (ch == '"')
                        containsQuote = true;
                    else if (ch == ',')
                        containsComma = true;
                }

                if (containsQuote && containsComma)
                    input = input.Replace("\"", "\"\"");

                if (containsComma)
                    return "\"" + input + "\"";
                else
                    return input;
            }
            catch
            {
                throw;
            }
        }


        private void btnDiagTool_click(object sender, RoutedEventArgs e)
        {
            KBGrid.Visibility = Visibility.Hidden;
            string path = @"C:\Program Files (x86)\UiPath\Studio";
            if (Directory.Exists(path))
            {

                string command = "UiPath.DiagTool.exe --file=C:\\logs.zip";
                GenerateCMDLogs(command);
                btnDisableTracing.IsEnabled = false;
                btnEnableTracing.IsEnabled = true;
                txtETLTips.Document.Blocks.Clear();
                txtETLTips.Document.Blocks.Add(new Paragraph(new Run("========= UiPath.DiagTool ==========" + Environment.NewLine + Environment.NewLine + "             Generated report: C:\\logs.zip" + Environment.NewLine + Environment.NewLine + "========= UiPath.DiagTool ==========")));

            }
            else
            {
                ErrorUSC = new MainWindowError("Error : Make sure UiPath is installed!", "Make sure UiPath is installed!", true);
                ETLDiagGrid.Children.Add(ErrorUSC);
            }


        }

        private void btnExportLogs_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                DataTable dt = dtlogs;
                IEnumerable<string> columnNames = dt.Columns.Cast<DataColumn>().
                                      Select(column => column.ColumnName);
                sb.AppendLine(string.Join(",", columnNames));
                foreach (DataRow dr in dt.Rows)
                {
                    foreach (DataColumn dc in dt.Columns)
                        sb.Append(FormatCSV(dr[dc.ColumnName].ToString()) + ",");
                    sb.Remove(sb.Length - 1, 1);
                    sb.AppendLine();
                }
                string path = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                File.WriteAllText(path + "\\EventViewerLogs.csv", sb.ToString());
                ErrorUSC = ErrorUSC = new MainWindowError("Success..!", "Logs has been exported successfully on Desktop." + Environment.NewLine + "File Name : EventViewerLogs.CSV", true);
                EventViewerGridMain.Children.Add(ErrorUSC);
            }
            catch (Exception ex)
            {
                ErrorUSC = new MainWindowError("Error : Error while exporting the logs", ex.Message, true);
                EventViewerGridMain.Children.Add(ErrorUSC);
            }
        }

        private void btnKBPatch_click(object sender, RoutedEventArgs e)
        {
            string[] knownAffectedKBPatchList = ConfigurationManager.AppSettings["KnownAffectedKBPatchList"].Split(',');

            KBGrid.Visibility = Visibility.Hidden;
            string command = "wmic qfe list brief /format:texttablewsys > \"C:\\KBPatchList_%date:~4,2%%date:~7,2%%date:~12,2%_%time:~0,2%%time:~3,2%%time:~6,2%.txt\"";
            //GenerateCMDLogs(command);
            var proc1 = new ProcessStartInfo();
            // string anyCommand;
            proc1.UseShellExecute = true;

            proc1.FileName = "cmd.exe";
            proc1.Verb = "runas";
            proc1.Arguments = "/c " + command;
            proc1.WindowStyle = ProcessWindowStyle.Hidden;
            Process.Start(proc1);

            var directory = new DirectoryInfo("C:\\");

            string fileName = directory.GetFiles()
             .OrderByDescending(f => f.LastWriteTime)
             .First().Name;

            if (fileName.Contains("KBPatchList_"))
            {
                DataTable tbl = new DataTable();


                for (int col = 0; col < 9; col++)
                    tbl.Columns.Add(new DataColumn("Column" + (col + 1).ToString()));



                //DataTable dt = new DataTable();
                //using (System.IO.TextReader tr = File.OpenText("C:\\" + fileName))
                //{
                //    string line;
                //    while ((line = tr.ReadLine()) != null)
                //    {

                //        string[] items = line.Trim().Split(',');
                //        if (dt.Columns.Count == 0)
                //        {
                //            // Create the data columns for the data table based on the number of items
                //            // on the first line of the file
                //            for (int i = 0; i < items.Length; i++)
                //                dt.Columns.Add(new DataColumn("Column" + i, typeof(string)));
                //        }
                //        dt.Rows.Add(items);

                //    }

                //}

                DataTable table = new DataTable("table_name");
                table.Columns.Add("Description", typeof(String));
                table.Columns.Add("HotFixID", typeof(String));
                table.Columns.Add("InstalledBy", typeof(String));
                table.Columns.Add("InstalledOn", typeof(String));
                table.Columns.Add("AffectedKB", typeof(String));

                //start reading the textfile
                StreamReader reader = new StreamReader("C:\\" + fileName);
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    string[] items = line.Split(new string[] { "  ", "\t", "" }, StringSplitOptions.RemoveEmptyEntries);


                    //make sure it has 3 items
                    if (items.Length <= 5)
                    {
                        DataRow row = table.NewRow();
                        row["Description"] = items[0].Trim();
                        row["HotFixID"] = items[1].Trim();
                        row["InstalledBy"] = items[2].Trim();
                        row["InstalledOn"] = items[3].Trim();
                        table.Rows.Add(row);
                    }
                }
                reader.Close();
                reader.Dispose();


                //string[] lines = System.IO.File.ReadAllLines("C:\\"+fileName);

                //foreach (string line in lines)
                //{
                //    var cols = line.Split(':');

                //    //DataRow dr = tbl.NewRow();
                //    for (int cIndex = 0; cIndex < lines.Count(); cIndex++)
                //    {
                //        tbl.Rows.Add(lines[cIndex]);

                //    }

                //   // tbl.Rows.Add(dr);
                //}


                //txtETLTips.Document.Blocks.Clear();
                KBGrid.Visibility = Visibility.Visible;

                foreach (string affectedKB in knownAffectedKBPatchList)
                {
                    DataRow dr = table.Rows.Cast<DataRow>().Single(row => row["HotFixID"].ToString() == affectedKB); ;
                    dr["AffectedKB"] = "Affected";
                }

                //foreach (DataRow dr in table.Rows)
                //{
                //    if(dr["HotFixID"].ToString() == "KB4100347" || dr["HotFixID"].ToString() == "KB4343669" || dr["HotFixID"].ToString() == "KB4489868")
                //    {
                //        dr["AffectedKB"] = "Affected";
                //    }

                //}
                //DataRow dr = table.Rows.Cast<DataRow>().Single(row => row["HotFixID"].ToString() == "KB4100347");

                //foreach (DataRow row in table.Rows)
                //{
                //    object[] array = row.ItemArray;
                //   // for (int i = 0; i < array.Length - 1; i++)
                //   // {
                //        txtETLTips.Document.Blocks.Add(new Paragraph(new Run(array[0].ToString() +"\t" + array[1].ToString() + "\t" + array[2].ToString() + "\t" + array[3].ToString())));
                //        //txtETLTips.Document.Blocks.Add(array[i].ToString());
                //   // }
                //    //swExtLogFile.WriteLine(array[i].ToString());
                //}

                KBGrid.ItemsSource = table.DefaultView;

                txtETLTips.Document.Blocks.Clear();
                txtETLTips.Document.Blocks.Add(new Paragraph(new Run("KB Patch List has been downloaded" + Environment.NewLine + Environment.NewLine + "File Location : C:\\KBPatchList_xxxx.txt")));
            }
        }

        private void btnAffectedKB_Click(object sender, RoutedEventArgs e)
        {
            string[] knownAffectedKBPatchList = ConfigurationManager.AppSettings["KnownAffectedKBPatchList"].Split(',');
            txtETLTips.Document.Blocks.Clear();
            txtETLTips.Document.Blocks.Add(new Paragraph(new Run("-------------List Of Some Known Affected KB Patches-------------" + Environment.NewLine)));
            foreach (string affectedKB in knownAffectedKBPatchList)
            {
                txtETLTips.Document.Blocks.Add(new Paragraph(new Run(affectedKB)));
            }
        }

        private void btnRepairDotNet_click(object sender, RoutedEventArgs e)
        {
            KBGrid.Visibility = Visibility.Hidden;
            System.Diagnostics.Process.Start("https://support.microsoft.com/en-in/help/2698555/microsoft-net-framework-repair-tool-is-available");
            txtETLTips.Document.Blocks.Clear();
            txtETLTips.Document.Blocks.Add(new Paragraph(new Run("Installation Instructions" + Environment.NewLine + Environment.NewLine + "Download Microsoft .NET Framework Repair Tool from the website. Run the same once downloaded.")));
        }

        #endregion

        // <summary>
        ///   Looks up a localized string similar to Log on as a batch job.
        /// </summary>
        internal static string SeBatchLogonRight
        {
            get
            {
                return "Log on as a batch job";// ResourceManager.GetString("SeBatchLogonRight", resourceCulture);
            }
        }



    }



    public class DDL_Level
    {
        public string Level_Name
        {
            get;
            set;
        }
        public Boolean Check_LevelStatus
        {
            get;
            set;
        }
    }

    public class DDL_Source
    {
        public string Source_Name
        {
            get;
            set;
        }
        public Boolean Check_SourceStatus
        {
            get;
            set;
        }
    }


}
