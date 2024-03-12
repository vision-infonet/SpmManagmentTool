using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Configuration.Install;
using System.Linq;
using System.Net;
using System.IO;
using System.Windows;

namespace SpmManagmentTool
{
    [RunInstaller(true)]
    public partial class SpmManagerInstaller : Installer
    {
        public SpmManagerInstaller()
        {
            InitializeComponent();
        }

        public override void Install(IDictionary stateSaver)
        {
            //FileStream _fs = null;
            //TextWriter _tw = null;
            //base.Install(stateSaver);
            //try
            //{
            //    if (File.Exists(@"C:\NextGenFuel\SpmManager\InstallRecord.txt"))
            //        File.Delete(@"C:\NextGenFuel\SpmManager\InstallRecord.txt");
            //    _fs = System.IO.File.Open(@"C:\NextGenFuel\SpmManager\InstallRecord.txt", System.IO.FileMode.CreateNew);
            //    _tw = new StreamWriter(_fs);
            //    _tw.WriteLine("Starting Intallation");
            //    string targetDirectory = Context.Parameters["assemblypath"];
            //    _tw.WriteLine("Target directory: " + targetDirectory);
            //    _tw.Flush();

            //    System.Configuration.Configuration config = System.Configuration.ConfigurationManager.OpenExeConfiguration(targetDirectory);
            //    config.AppSettings.Settings["ServerName"].Value = Dns.GetHostName();
            //    _tw.WriteLine(config.AppSettings.Settings["ServerName"].Value);
            //    IPAddress[] _ipaddresses = Dns.GetHostAddresses(Dns.GetHostName());
            //    if (_ipaddresses.Length > 0)
            //    {
            //        foreach (IPAddress ipaddress in _ipaddresses)
            //        {
            //            if (ipaddress.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork
            //               && ipaddress.ToString() != "127.0.0.1")
            //            {
            //                config.AppSettings.Settings["OPT_IP_address"].Value = ipaddress.ToString();
            //                _tw.WriteLine("Selected IP Address: " + ipaddress.ToString());
            //                break;
            //            }
            //        }
            //    }
            //    _tw.Flush();
            //    config.Save();

            //}
            //catch (Exception ex)
            //{
            //    if (null != _fs && null != _tw)
            //    {
            //        _tw.WriteLine("Error in install phase: " + ex.ToString());
            //        _tw.Flush();
            //    }
            //}
            //finally
            //{
            //    if (null != _tw)
            //        _tw.Close();
            //    if (null != _fs)
            //        _fs.Close();
            //}
        }
    }
}
