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
using SmartSupport.User_Controls.Dashboard;
using SmartSupport.User_Controls.ConfigFile;
using SmartSupport.User_Controls.Logging;
using SmartSupport.User_Controls.Error;
using System.Diagnostics;

namespace SmartSupport
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        UserControl usc = null;
        UserControl ErrorUSC = null;
        int count = 0;
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            #region Loading Dashboard User Control        
            GridMain.Children.Clear();
            var firstButton = ((Control)ListViewMenu.Items[0]).Name;
            usc = new ItemDashboard();
            GridMain.Children.Add(usc);
            
            #endregion
        }

        private void ListViewMenu_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (count == 0)
            {
                count++;
            }
            else
            {
                try
                {
                    GridMain.Children.Clear();

                    switch (((ListViewItem)((ListView)sender).SelectedItem).Name)
                    {
                        case "ItemDashboard":
                            usc = new ItemDashboard();
                            GridMain.Children.Add(usc);
                            break;
                        case "ItemConfigFile":
                            usc = new ItemConfigFile();
                            GridMain.Children.Add(usc);
                            break;
                        case "Logging":
                            usc = new Logging();
                            GridMain.Children.Add(usc);
                            break;
                        default:
                            break;
                    }
                }
                catch (Exception ex)
                {

                    ErrorUSC = new MainWindowError("Error : Problem loading the Ueser Controls", ex.Message, false);
                    SmartSupportWindow.Children.Add(ErrorUSC);
                }
            }
        }
    }



}
