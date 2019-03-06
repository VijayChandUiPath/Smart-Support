using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace UiPath.Monitoring.Server
{
    class ConfigFileDetails
    {
        public string fileName;
        public string fileContent;

        public static bool CreateFileBackUp(string sourceFileName)
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
                    destFileName = System.IO.Path.Combine(sourceDirectory, filenameWithoutExtension + suffix + fileExtension);
                    if (File.Exists(destFileName))
                        suffix++;
                    else
                        break;
                }
                File.Copy(sourceFileName, destFileName);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error : Can not create backup of file : " + ex.Message);
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
            catch (Exception ex)
            {
                Console.WriteLine("Error:Can not restart UiPath Robot service : " + ex.Message);
            }
        }

    }
}
