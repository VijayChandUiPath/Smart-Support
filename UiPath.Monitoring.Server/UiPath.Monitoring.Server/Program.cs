using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Collections;
using System.Threading.Tasks;
using System.Threading;
using System.Web.Script.Serialization;
using System.IO;
using UiPath.Monitoring.Server.Dashboard;

namespace UiPath.Monitoring.Server
{
    class Program
    {
        static Dictionary<string, Process> exesDict = new Dictionary<string, Process>();
        public static Dictionary<string, DashboardDetails> exeDetailDict = new Dictionary<string, DashboardDetails>();
        public static SystemInfo systemInfo = new SystemInfo();
        
        static void Main(string[] args)
        {

            // Start the exe monitoring thread
            ThreadStart childref = new ThreadStart(GetUiPathExes);
            Thread childThread = new Thread(childref);
            childThread.Start();
            //GetUiPathExes();


            //Console.WriteLine("Provide server(local) IP address : ");
            //string ip = Console.ReadLine();

            //Console.WriteLine("Provide Port : ");
            //int port = Convert.ToInt32(Console.ReadLine());

            string ip = "localhost";
            int port = 11000;

            while (true)
            {
                StartServer(ip, port); 
            }

            // childThread.Abort();

            Console.WriteLine("Press any key to exit...");
            Console.Read();
        }

        static void StartServer(string ip, int port)
        {
            IPAddress ipAddress = null;

            try
            {
                // Get host entries
                IPHostEntry host = Dns.GetHostEntry(ip);                         

                // Printing all the hosts 
                for (int i = 0; i < host.AddressList.Length; i++)
                {
                    Console.WriteLine(host.AddressList[i]);

                    //if(host.AddressList[i].ToString().Contains(ip))
                    //{
                    //    ipAddress = host.AddressList[i];
                    //}
                }
                ipAddress = host.AddressList[0];

            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
                return;
            }

            if(ipAddress == null)
            {
                Console.WriteLine("Host names does not contain the " + ip + " ip");
            }
            else
            {            
                try
                {
                    IPEndPoint localEndPoint = new IPEndPoint(ipAddress, port);

                    Socket listener = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

                    listener.Bind(localEndPoint);

                    listener.Listen(10);

                    Console.WriteLine("Waiting for a connection from " + localEndPoint.ToString() + " ...");
                    Socket handler = listener.Accept();

                    // Incoming data from the client.    
                    string data = null;
                    byte[] bytes = null;

                    while (true)
                    {
                        bytes = new byte[1024];
                        int bytesRec = 0;
                        handler.ReceiveTimeout = 1000;
                        try
                        {
                            bytesRec = handler.Receive(bytes);
                        }
                        catch (SocketException socEx)
                        {
                            //  If the time-out period is exceeded, the Receive method will throw a SocketException.                         
                        }

                        if (bytesRec > 0)
                            data = Encoding.ASCII.GetString(bytes, 0, bytesRec);
                        Console.WriteLine("Command received : {0}", data);

                        Commands command;
                        Enum.TryParse(data, out command);

                        try
                        {
                            switch (command)
                            {
                                case Commands.Dashboard:
                                    {                                        
                                        string jsonData = getDashboard();
                                        File.AppendAllText(@"log.txt", jsonData + Environment.NewLine + "=================================");
                                        byte[] msg = Encoding.ASCII.GetBytes(jsonData);
                                        handler.Send(msg);
                                        Console.WriteLine("Bytes Sent : " + msg.Count());
                                        break;
                                    }


                                case Commands.ConfigFiles:
                                    {
                                        string jsonData = getAllConfigFiles();
                                        byte[] msg = Encoding.ASCII.GetBytes(jsonData);
                                        handler.Send(msg);
                                        Console.WriteLine("Bytes Sent : " + msg.Count());
                                        command = Commands.DoNothing;
                                        break;
                                    }

                                case Commands.DoNothing:
                                    {
                                        break;
                                    }

                                case Commands.Stop:
                                    {
                                        handler.Shutdown(SocketShutdown.Both);
                                        handler.Close();
                                        break;
                                    }

                                default:
                                    break;

                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Error in drafting the reponse : " + ex.Message);
                            handler.Shutdown(SocketShutdown.Both);
                            handler.Close();
                            listener.Close();
                            throw ex;
                        }
                        Thread.Sleep(1000);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }                             
            }
        }

        // Thread - 1 : 
        // 1. Get UiPath exes 
        // 2. Check if new exe is up or some old exe has gone down
        // 3. Old exe has gone down --> remove that exe from exeDetailDict
        // 4. New exe has come up --> add that exe to exeDetailDict
        // 5. Take lock on exesDict and set it to currentExesDict
        static void GetUiPathExes()
        {
            while (true)
            {
                System.Diagnostics.Process[] processlist = Process.GetProcesses();
                Dictionary<string, Process> currentExesDict = new Dictionary<string, Process>();
                foreach (Process proc in processlist)
                {
                    if (proc.ProcessName.Contains("UiPath"))
                    {
                        currentExesDict.Add(proc.ProcessName, proc);

                        #region May be used later

                        //string fileName = theprocess.MainModule.FileName;
                        //FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(fileName);
                        //string company = fvi.CompanyName;
                        // if (company.Contains("UiPath"))
                        #endregion

                    }
                }

                // Checking if two dictionaries are same
                bool dictionariesEqual = exesDict.Keys.Count == currentExesDict.Keys.Count && exesDict.Keys.All(currentExesDict.ContainsKey);

                // Check if new exe is up or some old exe has gone down
                if (!dictionariesEqual)
                {
                    IEnumerable<string> keys_Current_Except_Keys_Exes = currentExesDict.Keys.Except(exesDict.Keys);
                    IEnumerable<string> keys_Exes_Except_Keys_Current = exesDict.Keys.Except(currentExesDict.Keys);

                    lock (exeDetailDict)
                    {
                        // Old exe has gone down --> remove that exe from exeDetailDict
                        foreach (string key in keys_Exes_Except_Keys_Current)
                        {
                            exeDetailDict.Remove(key);
                        }

                        // New exe has come up --> add that exe to exeDetailDict
                        foreach (string key in keys_Current_Except_Keys_Exes)
                        {
                            DashboardDetails exeDetailTemp = new DashboardDetails(currentExesDict[key]);
                            exeDetailDict.Add(key, exeDetailTemp);

                        }

                        // Take lock on exesDict and set it to currentExesDict                                 
                        exesDict = currentExesDict;
                    }

                }
                Thread.Sleep(1000);
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

            foreach (KeyValuePair<string, DashboardDetails> exe in exeDetailDict)
            {
                List<string> myFiles = (Directory.GetFiles(exe.Value.getExeFolder(), "*.*", SearchOption.AllDirectories).Where(s => ext.Contains(System.IO.Path.GetExtension(s)))).ToList();
                foreach(string file in myFiles)
                {
                    var temp = new Dictionary<string, string> { { file, System.IO.File.ReadAllText(file) } };
                    ls.Add(new ConfigFileDetails()
                    {
                        processName = exe.Key,
                        file = temp
                    });
                    
                }
            }
            string jsonData = JsonConvert.SerializeObject(ls);
            return jsonData;
        }
        enum Commands
        {
            Dashboard ,
            ConfigFiles,
            DoNothing,
            Stop
        }
    }
}
