using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
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
using Microsoft.AspNet.SignalR.Client;


namespace SmartSupport.User_Controls.Settings
{
    /// <summary>
    /// Interaction logic for Configuration.xaml
    /// </summary>
    public partial class Settings : UserControl
    {
        bool connected = false;
        public Settings()
        {
            InitializeComponent();
            DialogHost.IsOpen = true;
        }

        private void Sync_Click(object obj, RoutedEventArgs e)
        {

            try
            {
                string url = @"http://localhost:8080/";
                var connection = new HubConnection(url);
                MainWindow._hub = connection.CreateHubProxy("ServerHub");
                connection.Start().Wait();

                MainWindow._hub.On("ReceivePong", x => {
                    if (x == "Pong")
                    {
                        MainWindow.connected = true;
                        LanDisconnect.Visibility = Visibility.Hidden;
                        LanConnect.Visibility = Visibility.Visible;

                        Sync.Visibility = Visibility.Hidden;
                        SyncOff.Visibility = Visibility.Visible;


                        MainWindow.command = "Ping";
                    }
                        
                });
                MainWindow._hub.Invoke("Server", "Ping").Wait();

            }
            catch(Exception ex)
            {

            }

            #region old code
            //byte[] bytes = new byte[1024];

            //try
            //{
            //    string ip = "localhost";
            //    int port = 11000;
            //    IPHostEntry host = Dns.GetHostEntry(ip.Trim());

            //    IPAddress ipAddress = host.AddressList[0];
            //    IPEndPoint remoteEP = new IPEndPoint(ipAddress, port);

            //    // Create a TCP/IP  socket.    
            //    MainWindow.sender = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            //    try
            //    {
            //        // Connect to Remote EndPoint  
            //        MainWindow.sender.Connect(remoteEP);

            //        // Encode the data string into a byte array.    
            //        byte[] msg = Encoding.ASCII.GetBytes("Ping");

            //        // Send the data through the socket.    
            //        int bytesSent = MainWindow.sender.Send(msg);

            //        // Receive the response from the remote device.    
            //        int bytesRec = MainWindow.sender.Receive(bytes);
            //        string data = Encoding.ASCII.GetString(bytes, 0, bytesRec);
            //        Console.WriteLine(data);

            //        if(data.Contains("Pong"))
            //        {
            //            LanDisconnect.Visibility = Visibility.Hidden;
            //            LanConnect.Visibility = Visibility.Visible;

            //            Sync.Visibility = Visibility.Hidden;
            //            SyncOff.Visibility = Visibility.Visible;

            //            MainWindow.connected = true;
            //            MainWindow.command = "Ping";
            //        }
            //    }
            //    catch(ArgumentNullException ane)
            //    {
            //        Console.WriteLine("ArgumentNullException : {0}", ane.ToString());
            //    }
            //}
            //catch(Exception ex)
            //{
            //    Console.WriteLine(ex.ToString());
            //}

            #endregion
        }

        private void SyncOff_Click(object sender, RoutedEventArgs e)
        {

            MainWindow._hub.Invoke("Server", "Stop").Wait();
            MainWindow._hub.On("StopConnection", x => Console.WriteLine(x));

            LanConnect.Visibility = Visibility.Hidden;
            LanDisconnect.Visibility = Visibility.Visible;

            SyncOff.Visibility = Visibility.Hidden;
            Sync.Visibility = Visibility.Visible;

            MainWindow.connected = false;
            MainWindow.command = "DoNothing";


            #region old code
            //byte[] bytes = new byte[1024];
            //// Encode the data string into a byte array.    
            //byte[] msg = Encoding.ASCII.GetBytes("Stop");

            //// Send the data through the socket.    
            //int bytesSent = MainWindow.sender.Send(msg);

            //// Release the socket.
            ////MainWindow.sender.Shutdown(SocketShutdown.Both);
            ////MainWindow.sender.Disconnect(false);
            //MainWindow.sender.Close();

            //LanConnect.Visibility = Visibility.Hidden;
            //LanDisconnect.Visibility = Visibility.Visible;

            //SyncOff.Visibility = Visibility.Hidden;
            //Sync.Visibility = Visibility.Visible;

            //MainWindow.connected = false;
            //MainWindow.command = "DoNothing";
            #endregion
        }
    }
}
