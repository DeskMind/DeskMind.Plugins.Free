using DeskMind.Plugins.Office.Helpers;

using Microsoft.Office.Interop.Excel;
using Microsoft.Win32;

using System;
using System.IO;
using System.Runtime.InteropServices;

namespace DeskMind.Plugins.Office.Excel
{
    public static class ExcelHelper
    {
        public static Workbook GetOrOpenWorkbook(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException(filePath);

            Application excelApp;
            try
            {
                // Fix: Use Marshal.GetActiveObject from System.Runtime.InteropServices.ComTypes
                excelApp = (Application)Marshal2.GetActiveObject("Excel.Application");
            }
            catch (COMException)
            {
                // No Excel running, start new instance
                excelApp = new Application { Visible = true };
            }

            // Try to find workbook by full path
            foreach (Workbook wb in excelApp.Workbooks)
            {
                if (Path.GetFullPath(wb.FullName).Equals(Path.GetFullPath(filePath), StringComparison.OrdinalIgnoreCase))
                {
                    return wb;
                }
            }

            // Workbook not open, open it
            var openedWb = excelApp.Workbooks.Open(filePath);
            excelApp.Visible = true;
            return openedWb;
        }
    }
}

