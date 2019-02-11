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

namespace SmartSupport.User_Controls.Error
{
    /// <summary>
    /// Interaction logic for MainWindowError.xaml
    /// </summary>
    public partial class MainWindowError : UserControl
    {
        public MainWindowError(string caption, string message, bool showButton)
        {
            InitializeComponent();
            DialogHost.IsOpen = true;
            Caption.Text = caption;
            Error.Text = message;
            if (showButton)
                OkButton.Visibility = Visibility.Visible;
        }
    }
}
