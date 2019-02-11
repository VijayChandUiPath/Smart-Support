using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
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
using SmartSupport.User_Controls.Error;


namespace SmartSupport.User_Controls.ConfigFile
{
    /// <summary>
    /// Interaction logic for ItemConfigFile.xaml
    /// </summary>
    public partial class ItemConfigFile : UserControl
    {
        UserControl ErrorUSC = null;
        string exeFolder = "";
        string exePath = "";
        UserControl errorUSC;
        int comboExeSelectedIndex = 0;
        public ItemConfigFile()
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
                comboExeSelectedIndex = ComboExe.SelectedIndex;
                exePath = Process.GetProcessesByName(ComboExe.SelectedValue.ToString()).FirstOrDefault().MainModule.FileName;
                exeFolder = System.IO.Path.GetDirectoryName(Process.GetProcessesByName(ComboExe.SelectedValue.ToString()).FirstOrDefault().MainModule.FileName);
            }
            else
            {
                ErrorUSC = new MainWindowError("Error : No UiPath process is running...", "Please run the UiPath service from service manager", false);
                ConfigFile.Children.Add(ErrorUSC);
            }
            #endregion
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                ListViewItem lvi = null;
                configFiles.Items.Clear();
                var ext = new List<string> { ".config", ".Config", ".settings" };
                List<string> myFiles = (Directory.GetFiles(exeFolder, "*.*", SearchOption.AllDirectories).Where(s => ext.Contains(System.IO.Path.GetExtension(s)))).ToList();

                #region Checking folders --> appdata\Nuget, localappdata\UiPath, programdata\UiPath
                string appdataNuget = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Nuget");
                string localAppdataUiPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "UiPath");
                string programdataUiPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "UiPath");

                if (Directory.Exists(appdataNuget))
                {
                    List<string> temp = (Directory.GetFiles(appdataNuget, "*.*", SearchOption.AllDirectories).Where(s => ext.Contains(System.IO.Path.GetExtension(s)))).ToList();
                    myFiles.AddRange(temp);
                }
                if (Directory.Exists(localAppdataUiPath))
                {
                    List<string> temp = (Directory.GetFiles(localAppdataUiPath, "*.*", SearchOption.AllDirectories).Where(s => ext.Contains(System.IO.Path.GetExtension(s)))).ToList();
                    myFiles.AddRange(temp);
                }
                if (Directory.Exists(programdataUiPath))
                {
                    List<string> temp = (Directory.GetFiles(programdataUiPath, "*.*", SearchOption.AllDirectories).Where(s => ext.Contains(System.IO.Path.GetExtension(s)))).ToList();
                    myFiles.AddRange(temp);
                }
                #endregion


                myFiles.Sort();
                foreach (var file in myFiles)
                {
                    lvi = new ListViewItem();
                    lvi.ToolTip = file.ToString();
                    lvi.Content = System.IO.Path.GetFileName(file.ToString());
                    configFiles.Items.Add(lvi);
                }

                if (configFiles.Items.Count > 0)
                {
                    configFiles.SelectedIndex = 0;
                    comboExeSelectedIndex = ComboExe.SelectedIndex;
                }
            }
            catch (Exception ex)
            {
                ErrorUSC = new MainWindowError("Error : Problem in loading congif files", ex.Message, false);
                ConfigFile.Children.Add(ErrorUSC);
            }
        }

        private void ComboExe_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                exeFolder = System.IO.Path.GetDirectoryName(Process.GetProcessesByName(ComboExe.SelectedValue.ToString()).FirstOrDefault().MainModule.FileName);
            }
            catch (Exception ex)
            {
                ErrorUSC = new MainWindowError("Error : Exe no longer running : " + ComboExe.SelectedValue, ex.Message, true);
                Editor.Children.Add(ErrorUSC);
                //ComboExe.Items.Remove(ComboExe.SelectedValue);
                ComboExe.SelectedIndex = comboExeSelectedIndex;
                comboExeSelectedIndex = ComboExe.SelectedIndex;                
            }
            configFiles.Items.Clear();
            textEditor.Clear();
            ListViewItem lvi = null;  

            var ext = new List<string> { ".config", ".Config", ".settings" };
            List<string> myFiles = Directory.GetFiles(exeFolder, "*.*", SearchOption.AllDirectories).Where(s => ext.Contains(System.IO.Path.GetExtension(s))).ToList();

            #region Checking folders --> appdata\Nuget, localappdata\UiPath, programdata\UiPath
            string appdataNuget = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Nuget");
            string localAppdataUiPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "UiPath");
            string programdataUiPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "UiPath");

            if (Directory.Exists(appdataNuget))
            {
                List<string> temp = (Directory.GetFiles(appdataNuget, "*.*", SearchOption.AllDirectories).Where(s => ext.Contains(System.IO.Path.GetExtension(s)))).ToList();
                myFiles.AddRange(temp);
            }
            if (Directory.Exists(localAppdataUiPath))
            {
                List<string> temp = (Directory.GetFiles(localAppdataUiPath, "*.*", SearchOption.AllDirectories).Where(s => ext.Contains(System.IO.Path.GetExtension(s)))).ToList();
                myFiles.AddRange(temp);
            }
            if (Directory.Exists(programdataUiPath))
            {
                List<string> temp = (Directory.GetFiles(programdataUiPath, "*.*", SearchOption.AllDirectories).Where(s => ext.Contains(System.IO.Path.GetExtension(s)))).ToList();
                myFiles.AddRange(temp);
            }
            #endregion

            myFiles.Sort();
            foreach (var file in myFiles)
            {
                lvi = new ListViewItem();
                lvi.ToolTip = file.ToString();
                lvi.Content = System.IO.Path.GetFileName(file.ToString());
                configFiles.Items.Add(lvi);
            }

            if(configFiles.Items.Count > 0)
            {
                configFiles.SelectedIndex = 0;
                comboExeSelectedIndex = ComboExe.SelectedIndex;
            }

        }

        private void configFiles_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                int temp = configFiles.Items.Count;
                if(configFiles.Items.Count != 0)
                {
                    exePath = ((ListViewItem)((ListView)sender).SelectedItem).ToolTip.ToString();
                    textEditor.Load(exePath);
                    saveButton.IsEnabled = false;
                    title.Text = exePath;
                }
                
            }
            catch (Exception ex)
            {
                ErrorUSC = new MainWindowError("Error : Problem in loading congif files : " + exePath, ex.Message, false);
                Editor.Children.Add(ErrorUSC);
            }
        }

        private void textEditor_TextChanged(object sender, EventArgs e)
        {
            if (textEditor.LineCount != 0)
            {
                title.Text = exePath + "*";
                saveButton.IsEnabled = true;
            }
            
        }

        private void saveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                bool flag = CreateFileBackUp(exePath);
                if (flag)
                {
                    textEditor.Save(exePath);
                    title.Text = exePath;
                    ErrorUSC = ErrorUSC = new MainWindowError("Success..!", "File has been successfully saved." + Environment.NewLine + "Robot Service has been restarted...", true);
                    Editor.Children.Add(ErrorUSC);
                    saveButton.IsEnabled = false;
                    RestartWindowsService("UiRobotSvc");
                }
            }
            catch (Exception ex)
            {
                errorUSC = new MainWindowError("Error : Can not save file: " + exePath, ex.Message, true);
                Editor.Children.Add(errorUSC);
            }
        }

        private bool CreateFileBackUp(string sourceFileName)
        {
            try
            {
                //get the directory name the file is in
                string sourceDirectory = System.IO.Path.GetDirectoryName(sourceFileName);

                //get the file name without extension
                string filenameWithoutExtension = System.IO.Path.GetFileNameWithoutExtension(sourceFileName);

                //get the file extension
                string fileExtension = System.IO.Path.GetExtension(sourceFileName);

                int suffix = 1;
                string destFileName = String.Empty;
                while (true)
                {
                    //get the new file name you want,the Combine method is strongly recommended
                    destFileName = System.IO.Path.Combine(sourceDirectory, filenameWithoutExtension  + fileExtension + suffix);
                    if (File.Exists(destFileName))
                        suffix++;
                    else
                        break;
                }
                File.Copy(sourceFileName, destFileName);
            }
            catch (Exception ex)
            {
                errorUSC = new MainWindowError("Error : Can not create backup of file: " + sourceFileName, ex.Message, true);
                Editor.Children.Add(errorUSC);
                return false;
            }
            return true;
        }

        private void RestartWindowsService(string serviceName)
        {
            ServiceController serviceController = new ServiceController(serviceName);
            try
            {
                if ((serviceController.Status.Equals(ServiceControllerStatus.Running)) || (serviceController.Status.Equals(ServiceControllerStatus.StartPending)))
                {
                    serviceController.Stop();
                }
                serviceController.WaitForStatus(ServiceControllerStatus.Stopped);
                serviceController.Start();
                serviceController.WaitForStatus(ServiceControllerStatus.Running);
            }
            catch(Exception ex)
            {
                UserControl errorUSC = new MainWindowError("Error:Can not restart UiPath Robot service", ex.Message, true);
                ConfigFiles.Children.Add(errorUSC);
            }
        }
    }
}
