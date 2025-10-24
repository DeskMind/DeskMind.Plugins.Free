using Microsoft.Win32;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeskMind.Plugins.Office.Helpers
{
    public class GeneralHelpers
    {
        public static bool IsOfficeInstalled(string appName)
        {
            // Check for 64-bit Office on 64-bit Windows
            string registryKey = @$"SOFTWARE\Microsoft\Office\16.0\{appName}\InstallRoot";
            using (var key = Registry.LocalMachine.OpenSubKey(registryKey))
            {
                if (key != null)
                {
                    var path = key.GetValue("Path") as string;
                    if (!string.IsNullOrEmpty(path)) return true;
                }
            }

            // Check for 32-bit Office on 64-bit Windows
            registryKey = @$"SOFTWARE\WOW6432Node\Microsoft\Office\16.0\{appName}\InstallRoot";
            using (var key = Registry.LocalMachine.OpenSubKey(registryKey))
            {
                if (key != null)
                {
                    var path = key.GetValue("Path") as string;
                    if (!string.IsNullOrEmpty(path)) return true;
                }
            }

            return false;
        }
    }
}

