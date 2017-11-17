﻿using System;
using System.Linq;
using System.Management;
using System.Threading;
using System.Windows.Input;
using Microsoft.Win32;

namespace GoogleAnalyticsClientDotNet.Utility
{
    public class DeviceInformationService : IDeviceInformationService
    {
        public string DeviceFamily
        {
            get { return string.Empty; }
        }

        public bool IsDesktop
        {
            get { return true; }
        }

        public bool IsMobile
        {
            get { return false; }
        }

        public bool IsTouchEnabled
        {
            get { return Tablet.TabletDevices.Cast<TabletDevice>().Any(dev => dev.Type == TabletDeviceType.Touch); }
        }

        public string ModelName
        {
            get; private set;
        } = "Not found";

        public string Name
        {
            get { return Environment.UserName; }
        }

        public string OperatingSystem
        {
            get { return Environment.OSVersion.Platform.ToString(); }
        }

        public string OperationSystemVersion
        {
            get { return $"{Environment.OSVersion.Version.Major}.{Environment.OSVersion.Version.Minor}"; }
        }

        public string SystemArchitecture
        {
            get { return Environment.GetEnvironmentVariable("PROCESSOR_ARCHITECTURE"); }
        }

        public string SystemManufacturer
        {
            get; private set;
        } = "Not found";

        private static string uniqueId;
        public string UniqueId
        {
            get
            {
                if (string.IsNullOrEmpty(uniqueId) == false)
                {
                    return uniqueId;
                }

                ManagementClass mc = new ManagementClass("win32_processor");
                ManagementObjectCollection moc = mc.GetInstances();

                foreach (ManagementObject mo in moc)
                {
                    if (uniqueId == "")
                    {
                        //Get only the first CPU's ID
                        uniqueId = mo.Properties["processorID"].Value.ToString();
                        break;
                    }
                }

                return uniqueId;
            }
        }

        #region IEVersion

        private static int ieVersion = 9;
        public int IEVersion
        {
            get
            {
                if (ieVersion > 0)
                {
                    return ieVersion;
                }

                var version = Registry.LocalMachine.OpenSubKey(@"Software\Microsoft\Internet Explorer").GetValue("Version");

                if (version == null)
                {
                    return 0;
                }
                else
                {
                    string verStr = version.ToString();
                    verStr = verStr.Substring(0, verStr.IndexOf("."));
                    ieVersion = int.Parse(verStr);
                    return ieVersion;
                }
            }
        }

        public int TridentVersion
        {
            get
            {
                switch (IEVersion)
                {
                    case 10:
                        return 6;
                    case 11:
                        return 7;
                    default:
                        return 5;
                }
            }
        }
        #endregion

        public DeviceInformationService()
        {
            SelectQuery query = new SelectQuery(@"Select * from Win32_ComputerSystem");

            //initialize the searcher with the query it is supposed to execute
            using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(query))
            {
                try
                {
                    //execute the query
                    ManualResetEvent mre = new ManualResetEvent(false);
                    Thread waitingThread = new Thread(() =>
                    {
                        foreach (ManagementObject process in searcher.Get())
                        {
                            //print system info
                            process.Get();
                            SystemManufacturer = $"{process["Manufacturer"]}";
                            ModelName = $"{process["Model"]}";
                        }
                        mre.Set();
                    });
                    waitingThread.Start();
                    mre.WaitOne(5000); // wait 5 seconds
                }
                catch (Exception)
                {

                }
            }
        }
    }
}