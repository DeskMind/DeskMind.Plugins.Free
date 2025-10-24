using DeskMind.Plugins.Office.Helpers;

using Microsoft.Office.Interop.Outlook;

using System;
using System.Runtime.InteropServices;

namespace DeskMind.Plugins.Office.Outlook
{
    internal class OutlookHelper
    {
        /// <summary>
        /// Returns the running Outlook Application instance,
        /// or starts a new instance if Outlook is not running.
        /// </summary>
        /// <returns>Outlook Application instance</returns>
        public static Application GetOrStartOutlook()
        {
            Application app = null;

            try
            {
                // Try to get the running instance
                app = Marshal2.GetActiveObject("Outlook.Application") as Application;
            }
            catch (COMException)
            {
                // Outlook not running, create new instance
                app = new Application();
            }
            catch (System.Exception ex)
            {
                // Any other exception
                throw new InvalidOperationException("Failed to access Outlook instance.", ex);
            }

            return app;
        }

        /// <summary>
        /// Checks whether Outlook is installed on the machine.
        /// </summary>
        public static bool IsOutlookInstalled()
        {
            try
            {
                var app = new Application();
                app.Quit();
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}

