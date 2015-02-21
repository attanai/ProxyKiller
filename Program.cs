using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;
using System.Windows.Forms;
using System.Security.Principal;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.IO;
using System.Security.AccessControl;


namespace ProxyCleanup
{
    class Program
    {
        //Local Machine Keys

        private static RegistryKey IEProxy;
        private static RegistryKey IE64Proxy;
        private static RegistryKey NLAProxy1;
        private static RegistryKey NLAProxy2;
        private static RegistryKey IEConnections;
        private static RegistryKey IE64Connections;




        //Log Path
        public static string dataPath = "C:\\ProxyKiller\\" + DateTime.Now.ToFileTime() + "\\";
        

        static void Main(string[] args)
        {
            
            //Check if user is admin
            if (!IsUserAdministrator())
            {
                MessageBox.Show("Please run this program as Administrator.");
                Environment.Exit(1);
            }

            //Make sure that folder exists
            Directory.CreateDirectory(dataPath);

            //Start Log File
            string logPath = dataPath + "ProxyKillerLog" + DateTime.Now.ToFileTime() + ".txt";
            Console.SetOut(new TextFileWriter(logPath));

            Console.WriteLine("Backing up Registry");

            //Back up Registry
            Directory.CreateDirectory(dataPath + "Registry Backup");
            if (File.Exists(dataPath + "Registry Backup\\HKLM.reg"))
            {
                File.Delete(dataPath + "Registry Backup\\HKLM.reg");
            }
            if (File.Exists(dataPath + "Registry Backup\\HKU.reg"))
            {
                File.Delete(dataPath + "Registry Backup\\HKU.reg");
            }
            ExportKey("HKEY_LOCAL_MACHINE", dataPath + "Registry Backup\\HKLM.reg");
            ExportKey("HKEY_USERS", dataPath + "Registry Backup\\HKU.reg");


            //Remove HKLM Proxies
            bool local = RemoveProxiesLocalMachine();

            //Remove User Proxies

            //Get list of users
            RegistryKey OurKey = Registry.Users;
            List<string> users = GetSubKeys(OurKey);
            List<bool> proxies = new List<bool>();

            //Check Proxies for each user
            foreach (string s in users)
            {
                bool test = checkAndRemoveSingleUserProxy(s);
                proxies.Add(test);
            }

            //Summary
            string pup = "";
            Console.WriteLine("");
            Console.WriteLine("");
            Console.WriteLine("");
            Console.WriteLine("");
            Console.WriteLine("");
            Console.WriteLine("");
            Console.WriteLine("");
            Console.WriteLine("Summary:");
            if (local)
            {
                Console.WriteLine("Potential Proxies found and cleaned in Registry settings for Local Machine.");
                pup = pup + "Potential Proxies found and cleaned in Registry settings for Local Machine.\n\r";
            }
            else
            {
                Console.WriteLine("No Proxies Found in Registry Settings for Local Machine.");
            }


            for (int i = 0; i < users.Count; i++ )
            {
                if (proxies[i])
                {
                    Console.WriteLine("Potential Proxies found and cleaned in Registry settings for " + users[i] + ".");
                    pup = pup + "Potential Proxies found and cleaned in Registry settings for " + users[i] + ".\n\r";
                }
                else
                {
                    Console.WriteLine("No Proxies Found in Registry Settings for " + users[i] + ".");
                }
            }

            //End Program.
            Console.WriteLine("");
            Console.WriteLine("");

            //Alert User if Proxy is found
            if (pup != "")
            {
                MessageBox.Show(pup, "Potential Proxies Found!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }

            //Show Log
            RunCommand.run("notepad", logPath);

            

        }

        /// <summary>
        /// Checks for and removes Proxies in a single user. Must be running as Administrator.
        /// </summary>
        private static bool checkAndRemoveSingleUserProxy(string user)
        {
            //User Keys
            RegistryKey IEProxyD;
            RegistryKey IE64ProxyD;
            RegistryKey NLAProxy1D;
            RegistryKey NLAProxy2D;
            RegistryKey IEConnectionsD;
            RegistryKey IE64ConnectionsD;

            IEProxyD = Registry.Users.OpenSubKey(user + "\\Software\\Microsoft\\Windows\\CurrentVersion\\Internet Settings", RegistryKeyPermissionCheck.ReadWriteSubTree, RegistryRights.ChangePermissions | RegistryRights.ReadKey);
            IE64ProxyD = Registry.Users.OpenSubKey(user + "\\SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\Internet Settings", RegistryKeyPermissionCheck.ReadWriteSubTree, RegistryRights.ChangePermissions | RegistryRights.ReadKey);
            NLAProxy1D = Registry.Users.OpenSubKey(user + "\\SYSTEM\\ControlSet001\\services\\NlaSvc\\Parameters\\Internet\\ManualProxies", RegistryKeyPermissionCheck.ReadWriteSubTree, RegistryRights.ChangePermissions | RegistryRights.ReadKey);
            NLAProxy2D = Registry.Users.OpenSubKey(user + "\\SYSTEM\\ControlSet002\\services\\NlaSvc\\Parameters\\Internet\\ManualProxies", RegistryKeyPermissionCheck.ReadWriteSubTree, RegistryRights.ChangePermissions | RegistryRights.ReadKey);
            IEConnectionsD = Registry.Users.OpenSubKey(user + "\\Software\\Microsoft\\Windows\\CurrentVersion\\Internet Settings\\Connections", RegistryKeyPermissionCheck.ReadWriteSubTree, RegistryRights.ChangePermissions | RegistryRights.ReadKey);
            IE64ConnectionsD = Registry.Users.OpenSubKey(user + "\\Software\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\Internet Settings\\Connections", RegistryKeyPermissionCheck.ReadWriteSubTree, RegistryRights.ChangePermissions | RegistryRights.ReadKey);

            //Set Permissions to change owner
            string username = "Administrator";
            System.Security.AccessControl.RegistrySecurity rs = new System.Security.AccessControl.RegistrySecurity();
            rs.AddAccessRule(
                new RegistryAccessRule(
                    username,
                    RegistryRights.FullControl,
                    InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
                    PropagationFlags.InheritOnly,
                    AccessControlType.Allow));

            //The only reason for this to not run, if Admin, is if the Key doesn't exist.
            try { IEProxyD.SetAccessControl(rs); }
            catch { Console.WriteLine("No Key Found at .Default\\Software\\Microsoft\\Windows\\CurrentVersion\\Internet Settings"); }
            try { IE64ProxyD.SetAccessControl(rs); }
            catch { Console.WriteLine("No Key Found at .Default\\SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\Internet Settings"); }
            try { NLAProxy1D.SetAccessControl(rs); }
            catch { Console.WriteLine("No Key Found at .Default\\SYSTEM\\ControlSet001\\services\\NlaSvc\\Parameters\\Internet\\ManualProxies"); }
            try { NLAProxy2D.SetAccessControl(rs); }
            catch { Console.WriteLine("No Key Found at .Default\\SYSTEM\\ControlSet002\\services\\NlaSvc\\Parameters\\Internet\\ManualProxies"); }
            try { IEConnectionsD.SetAccessControl(rs); }
            catch { Console.WriteLine("No Key Found at .Default\\Software\\Microsoft\\Windows\\CurrentVersion\\Internet Settings\\Connections"); }
            try { IE64ConnectionsD.SetAccessControl(rs); }
            catch { Console.WriteLine("No Key Found at .Default\\Software\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\Internet Settings\\Connections"); }

            //Open Key with Full Access
            IEProxyD = Registry.Users.OpenSubKey(user + "\\Software\\Microsoft\\Windows\\CurrentVersion\\Internet Settings", RegistryKeyPermissionCheck.ReadWriteSubTree, RegistryRights.FullControl);
            IE64ProxyD = Registry.Users.OpenSubKey(user + "\\SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\Internet Settings", RegistryKeyPermissionCheck.ReadWriteSubTree, RegistryRights.FullControl);
            NLAProxy1D = Registry.Users.OpenSubKey(user + "\\SYSTEM\\ControlSet001\\services\\NlaSvc\\Parameters\\Internet\\ManualProxies", RegistryKeyPermissionCheck.ReadWriteSubTree, RegistryRights.FullControl);
            NLAProxy2D = Registry.Users.OpenSubKey(user + "\\SYSTEM\\ControlSet002\\services\\NlaSvc\\Parameters\\Internet\\ManualProxies", RegistryKeyPermissionCheck.ReadWriteSubTree, RegistryRights.FullControl);
            IEConnectionsD = Registry.Users.OpenSubKey(user + "\\Software\\Microsoft\\Windows\\CurrentVersion\\Internet Settings\\Connections", RegistryKeyPermissionCheck.ReadWriteSubTree, RegistryRights.FullControl);
            IE64ConnectionsD = Registry.Users.OpenSubKey(user + "\\Software\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\Internet Settings\\Connections", RegistryKeyPermissionCheck.ReadWriteSubTree, RegistryRights.FullControl);

            //Set access control
            try { IEProxyD.SetAccessControl(rs); }
            catch { }
            try { IE64ProxyD.SetAccessControl(rs); }
            catch { }
            try { NLAProxy1D.SetAccessControl(rs); }
            catch { }
            try { NLAProxy2D.SetAccessControl(rs); }
            catch { }
            try { IEConnectionsD.SetAccessControl(rs); }
            catch { }
            try { IE64ConnectionsD.SetAccessControl(rs); }
            catch { }

            //Check User settings
            bool op = false;
            Console.WriteLine("");
            Console.WriteLine("Checking Internet Explorer (32-bit) " + user + " for Proxy Settings.");
            if (IEProxyD.GetValue("ProxyEnable", "0").ToString() != "0")
            {
                Console.WriteLine("Proxy is Enabled for " + user + " in 32-bit Internet Explorer.");
                op = true;
            }
            else
            {
                Console.WriteLine("No proxy is Enabled for " + user + " in 32-bit Internet Explorer.");
            }
            if (IEProxyD.GetValue("ProxyOverride", "0").ToString() != "0")
            {
                Console.WriteLine("32-bit Internet Explorer Proxy Override Enabled for " + user + ".");
                op = true;
            }
            else
            {
                Console.WriteLine("No proxy override is Enabled for " + user + " in 32-bit Internet Explorer.");
            }
            if (IEProxyD.GetValue("ProxyServer", "NA").ToString() != "NA")
            {
                Console.WriteLine("Internet Explorer Proxy Found for " + user + " in 32-bit Internet Explorer: " + IEProxyD.GetValue("ProxyServer", "NA"));
                op = true;
            }
            else
            {
                Console.WriteLine("No proxy server is located for " + user + " in 32-bit Internet Explorer.");
            }

            try
            {
                if (IEConnectionsD.GetValue("DefaultConnectionSettings", "").ToString() != "")
                {
                    Console.WriteLine("Default connection settings found for " + user + " in 32-bit Internet Explorer");
                    op = true;
                }
                else
                {
                    Console.WriteLine("No Default Connection Settings found for " + user + " in 32-bit Internet Explorer.");
                }
            }
            catch
            {
                Console.WriteLine("No Default Connection Keys found for " + user + " in 32-bit Internet Explorer.");
            }
            try
            {
                if (IEConnectionsD.GetValue("SavedLegacySettings", "").ToString() != "")
                {
                    Console.WriteLine("Saved Legacy Settings found for " + user + " in 32-bit Internet Explorer.");
                    op = true;
                }
                else
                {
                    Console.WriteLine("No Saved Legacy Settings found for " + user + " in 32-bit Internet Explorer.");
                }
            }
            catch
            {
                Console.WriteLine("No Saved Legacy Keys found for " + user + " in 32-bit Internet Explorer.");
            }
            Console.WriteLine("");
            Console.WriteLine("Checking 32-bit Network Location Awareness Proxy Settings for " + user + ".");
            try
            {
                if (NLAProxy1D.GetValue("", "").ToString() != "")
                {
                    Console.WriteLine("Network Location Awareness (ControlSet001) Found for " + user + " in 32-bit Internet Explorer: " + NLAProxy1D.GetValue("Default", ""));
                    op = true;
                }
                else
                {
                    Console.WriteLine("No Proxy set in NLA for ControlSet001 for " + user + " in 32-bit Internet Explorer.");
                }
            }
            catch
            {
                Console.WriteLine("No NLA for ControlSet001 for " + user + " in 32-bit Internet Explorer.");
            }

            try
            {
                if (NLAProxy2D.GetValue("", "").ToString() != "")
                {
                    Console.WriteLine("Network Location Awareness (ControlSet002) Found for " + user + " in 32-bit Internet Explorer: " + NLAProxy2D.GetValue("Default", ""));
                    op = true;
                }
                else
                {
                    Console.WriteLine("No Proxy set in NLA for ControlSet002 for " + user + " in 32-bit Internet Explorer.");
                }
            }
            catch
            {
                Console.WriteLine("No NLA for ControlSet002 for " + user + " in 32-bit Internet Explorer.");
            }


            //Check 64-bit Settings
            try
            {
                Console.WriteLine("");
                Console.WriteLine("Checking Internet Explorer (64-bit) " + user + " for Proxy Settings.");
                if (IE64ProxyD.GetValue("ProxyEnable", "0").ToString() != "0")
                {
                    Console.WriteLine("Proxy is Enabled for " + user + " in 64-bit Internet Explorer.");
                    op = true;
                }
                else
                {
                    Console.WriteLine("No proxy is Enabled for " + user + " in 64-bit Internet Explorer.");
                }
                if (IE64ProxyD.GetValue("ProxyOverride", "0").ToString() != "0")
                {
                    Console.WriteLine("64-bit Internet Explorer Proxy Override Enabled for " + user + ".");
                    op = true;
                }
                else
                {
                    Console.WriteLine("No proxy override is Enabled for " + user + " in 64-bit Internet Explorer.");
                }
                if (IE64ProxyD.GetValue("ProxyServer", "NA").ToString() != "NA")
                {
                    Console.WriteLine("Internet Explorer Proxy Found for " + user + " in 64-bit Internet Explorer: " + IE64ProxyD.GetValue("ProxyServer", "NA"));
                    op = true;
                }
                else
                {
                    Console.WriteLine("No proxy server is located for " + user + " in 64-bit Internet Explorer.");
                }
                try
                {
                    if (IE64ConnectionsD.GetValue("DefaultConnectionSettings", "").ToString() != "")
                    {
                        Console.WriteLine("Default connection settings found for " + user + " in 64-bit Internet Explorer.");
                        op = true;
                    }
                    else
                    {
                        Console.WriteLine("No Default Connection Settings found for " + user + " in 64-bit Internet Explorer.");
                    }
                }
                catch
                {
                    Console.WriteLine("No Default Connection Key found for " + user + " in 64-bit Internet Explorer.");
                }
                try
                {
                    if (IE64ConnectionsD.GetValue("SavedLegacySettings", "").ToString() != "")
                    {
                        Console.WriteLine("Saved Legacy Settings found for " + user + " in 64-bit Internet Explorer.");
                        op = true;
                    }
                    else
                    {
                        Console.WriteLine("No Saved Legacy Settings found for " + user + " in 64-bit Internet Explorer.");
                    }
                }
                catch
                {
                    Console.WriteLine("No Saved Legacy Keys found for " + user + " in 64-bit Internet Explorer.");
                }
            }
            catch
            {
                Console.WriteLine("");
                Console.WriteLine("Skipping 64-bit Keys for " + user + ".");
            }

            if (!op)
            {
                Console.WriteLine("");
                Console.WriteLine("No Proxy Settings Found");
                Console.WriteLine("");
            }

            if (op)
            {
                //Kill Proxy Settings for " + user + "
                Console.WriteLine("");
                Console.WriteLine("Removing Proxy Settings for " + user + " in 32-bit Internet Explorer.");
                string[] IEPDs = IEProxyD.GetValueNames();
                if (IEPDs.Contains<string>("ProxyEnable"))
                {
                    IEProxyD.SetValue("ProxyEnable", "0");
                    Console.WriteLine("Disabled Proxy for " + user + " in 32-bit Internet Explorer.");
                }
                if (IEPDs.Contains<string>("ProxyOverride"))
                {
                    IEProxyD.DeleteValue("ProxyOverride");
                    Console.WriteLine("Deleted Proxy Override for " + user + " in 32-bit Internet Explorer.");
                }
                if (IEPDs.Contains<string>("ProxyServer"))
                {
                    IEProxyD.DeleteValue("ProxyServer");
                    Console.WriteLine("Deleted Proxy Server for " + user + " in 32-bit Internet Explorer.");
                }
                try
                {

                    try
                    {
                        try
                        {
                            IEConnectionsD.DeleteValue("DefaultConnectionSettings");
                        }
                        catch
                        {
                            IEConnectionsD.SetValue("DefaultConnectionSettings", "");
                        }
                        Console.WriteLine("Deleted Default Connection Settings for " + user + " in 32-bit Internet Explorer.");
                    }
                    catch
                    {
                        Console.WriteLine("Unable to Delete Default Connection Settings for " + user + " in 32-bit Internet Explorer.");
                    }

                    try
                    {

                        try
                        {
                            IEConnectionsD.DeleteValue("SavedLegacySettings");
                        }
                        catch
                        {
                            IEConnectionsD.SetValue("SavedLegacySettings", "");
                        }
                        Console.WriteLine("Deleted Saved Legacy Settings for " + user + " in 32-bit Internet Explorer.");

                    }
                    catch
                    {
                        Console.WriteLine("Unable to Delete Saved Legacy Settings for " + user + " in 32-bit Internet Explorer.");
                    }
                }
                catch
                {
                    Console.WriteLine("Unable to access .DEFAULT connection Strings.");
                }
                try
                {
                    NLAProxy1D.SetValue("(Default)", "");
                    Console.WriteLine("Deleted Value for NLA Proxy for " + user + " in ControlSet001.");
                }
                catch { }
                try
                {
                    NLAProxy2D.SetValue("(Default)", "");
                    Console.WriteLine("Deleted Value for NLA Proxy for " + user + " in ControlSet002.");
                }
                catch { }
                try
                {
                    NLAProxy1D.DeleteValue("(Default)");
                    Console.WriteLine("Deleted Key for NLA Proxy for " + user + " in ControlSet001.");
                }
                catch { }
                try
                {
                    NLAProxy2D.DeleteValue("(Default)");
                    Console.WriteLine("Deleted Key for NLA Proxy for " + user + " in ControlSet002.");
                }
                catch { }

                //64-bit keys for .DEFAULT
                try
                {
                    Console.WriteLine("");
                    Console.WriteLine("Removing Proxy Settings for " + user + " in 64-bit Internet Explorer.");
                    string[] IEP64Ds = IE64ProxyD.GetValueNames();
                    if (IEP64Ds.Contains<string>("ProxyEnable"))
                    {
                        IE64ProxyD.SetValue("ProxyEnable", "0");
                        Console.WriteLine("Disabled Proxy for " + user + " in 64-bit Internet Explorer.");
                    }
                    if (IEP64Ds.Contains<string>("ProxyOverride"))
                    {
                        IE64ProxyD.DeleteValue("ProxyOverride");
                        Console.WriteLine("Deleted Proxy Override for " + user + " in 64-bit Internet Explorer.");
                    }
                    if (IEP64Ds.Contains<string>("ProxyServer"))
                    {
                        IE64ProxyD.DeleteValue("ProxyServer");
                        Console.WriteLine("Deleted Proxy Server for " + user + " in 64-bit Internet Explorer.");
                    }


                    string[] IEPC64Ds = IE64ConnectionsD.GetValueNames();
                    if (IEPC64Ds.Contains<string>("DefaultConnectionSettings"))
                    {
                        IE64ConnectionsD.DeleteValue("DefaultConnectionSettings");
                        Console.WriteLine("Deleted Default Connection Settings for " + user + " in 64-bit Internet Explorer.");
                    }
                    if (IEPC64Ds.Contains<string>("SavedLegacySettings"))
                    {
                        IE64ConnectionsD.DeleteValue("SavedLegacySettings");
                        Console.WriteLine("Deleted Saved Legacy Settings for " + user + " in 64-bit Internet Explorer.");
                    }

                }
                catch
                {
                    Console.WriteLine("");
                    Console.WriteLine("Skipping 64-bit keys.");
                }
            }
            else
            {
                Console.WriteLine("");
                Console.WriteLine("No Proxy Information Found for User " + user);
            }

            return op;
        }

        /// <summary>
        /// Checks for and removes Proxies in HKLM. Must be running as Administrator.
        /// </summary>
        private static bool RemoveProxiesLocalMachine()
        {
            
            //HKLM

            //Set reg keys
            IEProxy = Registry.LocalMachine.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Internet Settings", RegistryKeyPermissionCheck.ReadWriteSubTree, RegistryRights.ChangePermissions | RegistryRights.ReadKey);
            IE64Proxy = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\Internet Settings", RegistryKeyPermissionCheck.ReadWriteSubTree, RegistryRights.ChangePermissions | RegistryRights.ReadKey);
            NLAProxy1 = Registry.LocalMachine.OpenSubKey("SYSTEM\\ControlSet001\\services\\NlaSvc\\Parameters\\Internet\\ManualProxies", RegistryKeyPermissionCheck.ReadWriteSubTree, RegistryRights.ChangePermissions | RegistryRights.ReadKey);
            NLAProxy2 = Registry.LocalMachine.OpenSubKey("SYSTEM\\ControlSet002\\services\\NlaSvc\\Parameters\\Internet\\ManualProxies", RegistryKeyPermissionCheck.ReadWriteSubTree, RegistryRights.ChangePermissions | RegistryRights.ReadKey);
            IEConnections = Registry.LocalMachine.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Internet Settings\\Connections", RegistryKeyPermissionCheck.ReadWriteSubTree, RegistryRights.ChangePermissions | RegistryRights.ReadKey);
            IE64Connections = Registry.LocalMachine.OpenSubKey("Software\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\Internet Settings\\Connections", RegistryKeyPermissionCheck.ReadWriteSubTree, RegistryRights.ChangePermissions | RegistryRights.ReadKey);

            //Set Permissions to change owner
            string user = "Administrator";
            System.Security.AccessControl.RegistrySecurity rs = new System.Security.AccessControl.RegistrySecurity();
            rs.AddAccessRule(
                new RegistryAccessRule(
                    user,
                    RegistryRights.FullControl,
                    InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
                    PropagationFlags.InheritOnly,
                    AccessControlType.Allow));


            //The only reason for this to not run, if Admin, is if the Key doesn't exist.
            try { IEProxy.SetAccessControl(rs); }
            catch { Console.WriteLine("No Key Found at Software\\Microsoft\\Windows\\CurrentVersion\\Internet Settings"); }
            try { IE64Proxy.SetAccessControl(rs); }
            catch { Console.WriteLine("No Key Found at SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\Internet Settings"); }
            try { NLAProxy1.SetAccessControl(rs); }
            catch { Console.WriteLine("No Key Found at SYSTEM\\ControlSet001\\services\\NlaSvc\\Parameters\\Internet\\ManualProxies"); }
            try { NLAProxy2.SetAccessControl(rs); }
            catch { Console.WriteLine("No Key Found at SYSTEM\\ControlSet002\\services\\NlaSvc\\Parameters\\Internet\\ManualProxies"); }
            try { IEConnections.SetAccessControl(rs); }
            catch { Console.WriteLine("No Key Found at Software\\Microsoft\\Windows\\CurrentVersion\\Internet Settings\\Connections"); }
            try { IE64Connections.SetAccessControl(rs); }
            catch { Console.WriteLine("No Key Found at Software\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\Internet Settings\\Connections"); }


            //Open Keys with Full Control
            IEProxy = Registry.LocalMachine.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Internet Settings", RegistryKeyPermissionCheck.ReadWriteSubTree, RegistryRights.FullControl);
            IE64Proxy = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\Internet Settings", RegistryKeyPermissionCheck.ReadWriteSubTree, RegistryRights.FullControl);
            NLAProxy1 = Registry.LocalMachine.OpenSubKey("SYSTEM\\ControlSet001\\services\\NlaSvc\\Parameters\\Internet\\ManualProxies", RegistryKeyPermissionCheck.ReadWriteSubTree, RegistryRights.FullControl);
            NLAProxy2 = Registry.LocalMachine.OpenSubKey("SYSTEM\\ControlSet002\\services\\NlaSvc\\Parameters\\Internet\\ManualProxies", RegistryKeyPermissionCheck.ReadWriteSubTree, RegistryRights.FullControl);
            IEConnections = Registry.LocalMachine.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Internet Settings\\Connections", RegistryKeyPermissionCheck.ReadWriteSubTree, RegistryRights.FullControl);
            IE64Connections = Registry.LocalMachine.OpenSubKey("Software\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\Internet Settings\\Connections", RegistryKeyPermissionCheck.ReadWriteSubTree, RegistryRights.FullControl);

            //Set security Owner
            rs.SetOwner(new NTAccount("Administrator"));

            try { IEProxy.SetAccessControl(rs); }
            catch { }
            try { IE64Proxy.SetAccessControl(rs); }
            catch { }
            try { NLAProxy1.SetAccessControl(rs); }
            catch { }
            try { NLAProxy2.SetAccessControl(rs); }
            catch { }
            try { IEConnections.SetAccessControl(rs); }
            catch { }
            try { IE64Connections.SetAccessControl(rs); }
            catch { }


            bool op = false;

            //Check Local Machine Settings
            Console.WriteLine("");
            Console.WriteLine("Checking Internet Explorer (32-bit) for Proxy Settings.");

            if (IEProxy.GetValue("ProxyEnable", "0").ToString() != "0")
            {
                Console.WriteLine("Proxy is Enabled for 32-bit Internet Explorer.");
                op = true;
            }
            else
            {
                Console.WriteLine("No proxy is Enabled for 32-bit Internet Explorer.");
            }
            if (IEProxy.GetValue("ProxyOverride", "0").ToString() != "0")
            {
                Console.WriteLine("32-bit Internet Explorer Proxy Override Enabled.");
                op = true;
            }
            else
            {
                Console.WriteLine("No proxy override is Enabled for 32-bit Internet Explorer.");
            }
            if (IEProxy.GetValue("ProxyServer", "NA").ToString() != "NA")
            {
                Console.WriteLine("Internet Explorer Proxy Found for 32-bit Internet Explorer: " + IEProxy.GetValue("ProxyServer", "NA"));
                op = true;
            }
            else
            {
                Console.WriteLine("No proxy server is located for 32-bit Internet Explorer.");
            }

            try
            {
                if (IEConnections.GetValue("DefaultConnectionSettings", "").ToString() != "")
                {
                    Console.WriteLine("Default connection settings found for 32-bit Internet Explorer.");
                    op = true;
                }
                else
                {
                    Console.WriteLine("No Default Connection Settings found for 32-bit Internet Explorer.");
                }
            }
            catch
            {
                Console.WriteLine("No Default Connection Keys found for 32-bit Internet Explorer.");
            }
            try
            {
                if (IEConnections.GetValue("SavedLegacySettings", "").ToString() != "")
                {
                    Console.WriteLine("Saved Legacy Settings found for 32-bit Internet Explorer.");
                    op = true;
                }
                else
                {
                    Console.WriteLine("No Saved Legacy Settings found for 32-bit Internet Explorer.");
                }
            }
            catch
            {
                Console.WriteLine("No Saved Legacy Keys found for 32-bit Internet Explorer.");
            }
            Console.WriteLine("");
            Console.WriteLine("Checking 32-bit Network Location Awareness Proxy Settings.");
            try
            {
                if (NLAProxy1.GetValue("", "").ToString() != "")
                {
                    Console.WriteLine("Network Location Awareness (ControlSet001) Found for 32-bit Internet Explorer: " + NLAProxy1.GetValue("Default", ""));
                    op = true;
                }
                else
                {
                    Console.WriteLine("No Proxy set in NLA for ControlSet001 for 32-bit Internet Explorer.");
                }
            }
            catch
            {
                Console.WriteLine("No NLA for ControlSet001 for 32-bit Internet Explorer.");
            }

            try
            {
                if (NLAProxy2.GetValue("", "").ToString() != "")
                {
                    Console.WriteLine("Network Location Awareness (ControlSet002) Found for 32-bit Internet Explorer: " + NLAProxy2.GetValue("Default", ""));
                    op = true;
                }
                else
                {
                    Console.WriteLine("No Proxy set in NLA for ControlSet002 for 32-bit Internet Explorer.");
                }
            }
            catch
            {
                Console.WriteLine("No NLA for ControlSet002 for 32-bit Internet Explorer.");
            }


            //Check 64-bit Settings
            try
            {
                Console.WriteLine("");
                Console.WriteLine("Checking Internet Explorer (64-bit) for Proxy Settings.");
                if (IE64Proxy.GetValue("ProxyEnable", "0").ToString() != "0")
                {
                    Console.WriteLine("Proxy is Enabled for 64-bit Internet Explorer.");
                    op = true;
                }
                else
                {
                    Console.WriteLine("No proxy is Enabled for 64-bit Internet Explorer.");
                }
                if (IE64Proxy.GetValue("ProxyOverride", "0").ToString() != "0")
                {
                    Console.WriteLine("64-bit Internet Explorer Proxy Override Enabled.");
                    op = true;
                }
                else
                {
                    Console.WriteLine("No proxy override is Enabled for 64-bit Internet Explorer.");
                }
                if (IE64Proxy.GetValue("ProxyServer", "NA").ToString() != "NA")
                {
                    Console.WriteLine("Internet Explorer Proxy Found for 64-bit Internet Explorer: " + IE64Proxy.GetValue("ProxyServer", "NA"));
                    op = true;
                }

                else
                {
                    Console.WriteLine("No proxy server is located for 64-bit Internet Explorer.");
                }
                try
                {
                    if (IE64Connections.GetValue("DefaultConnectionSettings", "").ToString() != "")
                    {
                        Console.WriteLine("Default connection settings found for 64-bit Internet Explorer.");
                        op = true;
                    }
                    else
                    {
                        Console.WriteLine("No Default Connection Settings found for 64-bit Internet Explorer.");
                    }
                }
                catch
                {
                    Console.WriteLine("No Default Connection Key found for 64-bit Internet Explorer.");
                }
                try
                {
                    if (IE64Connections.GetValue("SavedLegacySettings", "").ToString() != "")
                    {
                        Console.WriteLine("Saved Legacy Settings found for 64-bit Internet Explorer.");
                        op = true;
                    }
                    else
                    {
                        Console.WriteLine("No Saved Legacy Settings found for 64-bit Internet Explorer.");
                    }
                }
                catch
                {
                    Console.WriteLine("No Saved Legacy Keys found for 64-bit Internet Explorer.");
                }
            }
            catch
            {
                Console.WriteLine("");
                Console.WriteLine("Skipping 64-bit Keys.");
            }

            if (op)
            {
                //Kill 32-bit Proxy
                Console.WriteLine("");
                Console.WriteLine("Removing Proxy Settings for 32-bit Internet Explorer.");
                string[] IEPs = IEProxy.GetValueNames();
                if (IEPs.Contains<string>("ProxyEnable"))
                {
                    IEProxy.SetValue("ProxyEnable", "0");
                    Console.WriteLine("Disabled Proxy for 32-bit Internet Explorer.");
                }
                if (IEPs.Contains<string>("ProxyOverride"))
                {
                    IEProxy.DeleteValue("ProxyOverride");
                    Console.WriteLine("Deleted Proxy Override for 32-bit Internet Explorer.");
                }
                if (IEPs.Contains<string>("ProxyServer"))
                {
                    IEProxy.DeleteValue("ProxyServer");
                    Console.WriteLine("Deleted Proxy Server for 32-bit Internet Explorer.");
                }

                string[] IEPCs = IEConnections.GetValueNames();
                if (IEPCs.Contains<string>("DefaultConnectionSettings"))
                {
                    IEConnections.DeleteValue("DefaultConnectionSettings");
                    Console.WriteLine("Deleted Default Connection Settings for 32-bit Internet Explorer.");
                }
                if (IEPCs.Contains<string>("SavedLegacySettings"))
                {
                    IEConnections.DeleteValue("SavedLegacySettings");
                    Console.WriteLine("Deleted Saved Legacy Settings for 32-bit Internet Explorer.");
                }
                try
                {
                    NLAProxy1.SetValue("", "");
                    Console.WriteLine("Deleted Value for NLA Proxy for ControlSet001.");
                }
                catch { }
                try
                {
                    NLAProxy2.SetValue("", "");
                    Console.WriteLine("Deleted Value for NLA Proxy for ControlSet002.");
                }
                catch { }
                try
                {
                    NLAProxy1.DeleteValue("", true);
                    Console.WriteLine("Deleted Key for NLA Proxy for ControlSet001.");
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
                try
                {
                    NLAProxy2.DeleteValue("", true);
                    Console.WriteLine("Deleted Key for NLA Proxy for ControlSet002.");
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }

                //Attempt to Kill 64-bit Proxy
                try
                {
                    Console.WriteLine("");
                    Console.WriteLine("Removing Proxy Settings for 64-bit Internet Explorer.");
                    string[] IEP64s = IE64Proxy.GetValueNames();
                    if (IEP64s.Contains<string>("ProxyEnable"))
                    {
                        IE64Proxy.SetValue("ProxyEnable", "0");
                        Console.WriteLine("Disabled Proxy for 64-bit Internet Explorer.");
                    }
                    if (IEP64s.Contains<string>("ProxyOverride"))
                    {
                        IE64Proxy.DeleteValue("ProxyOverride");
                        Console.WriteLine("Deleted Proxy Override for 64-bit Internet Explorer.");
                    }
                    if (IEP64s.Contains<string>("ProxyServer"))
                    {
                        IE64Proxy.DeleteValue("ProxyServer");
                        Console.WriteLine("Deleted Proxy Server for 64-bit Internet Explorer.");
                    }


                    string[] IEPC64s = IE64Connections.GetValueNames();
                    if (IEPC64s.Contains<string>("DefaultConnectionSettings"))
                    {
                        IE64Connections.DeleteValue("DefaultConnectionSettings");
                        Console.WriteLine("Deleted Default Connection Settings for 64-bit Internet Explorer.");
                    }
                    if (IEPC64s.Contains<string>("SavedLegacySettings"))
                    {
                        IE64Connections.DeleteValue("SavedLegacySettings");
                        Console.WriteLine("Deleted Saved Legacy Settings for 64-bit Internet Explorer.");
                    }

                }
                catch
                {
                    Console.WriteLine("");
                    Console.WriteLine("Skipping 64-bit keys.");
                }
            }

            else
            {
                Console.WriteLine("");
                Console.WriteLine("No Proxy Information Found in HKLM");
            }

            return op;

        }

        /// <summary>
        /// Returns true if user is an Administrator
        /// </summary>
        /// <returns></returns>
        public static bool IsUserAdministrator()
        {
            //bool value to hold our return value
            bool isAdmin;
            try
            {
                //get the currently logged in user
                WindowsIdentity user = WindowsIdentity.GetCurrent();
                WindowsPrincipal principal = new WindowsPrincipal(user);
                isAdmin = principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch (UnauthorizedAccessException ex)
            {
                isAdmin = false;
            }
            catch (Exception ex)
            {
                isAdmin = false;
            }
            return isAdmin;
        }

        /// <summary>
        /// Exports Key to .Reg file.
        /// </summary>
        /// <param name="RegKey">Key to be exported.</param>
        /// <param name="SavePath">Path to file where key will be saved.</param>
        private static void ExportKey(string RegKey, string SavePath)
        {
            string path = "\"" + SavePath + "\"";
            string key = "\"" + RegKey + "\"";

            Process proc = new Process();
            try
            {
                proc.StartInfo.FileName = "regedit.exe";
                proc.StartInfo.UseShellExecute = false;
                proc = Process.Start("regedit.exe", "/e " + path + " " + key + "");

                if (proc != null) proc.WaitForExit();
            }
            finally
            {
                if (proc != null) proc.Dispose();
            }
        }

        /// <summary>
        /// Returns list of users in registry
        /// </summary>
        /// <param name="SubKey"></param>
        /// <returns></returns>
        private static List<string> GetSubKeys(RegistryKey SubKey)
        {
            List<string> names = new List<string>();
            foreach (string sub in SubKey.GetSubKeyNames())
            {
                if (!sub.Contains("Classes"))
                {
                    names.Add(sub);
                }
            }

            return names;
        }



    }
}
