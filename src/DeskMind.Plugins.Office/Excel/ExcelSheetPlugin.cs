using Microsoft.Office.Interop.Excel;
using Microsoft.SemanticKernel;

using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace DeskMind.Plugins.Office.Excel
{
    public class ExcelSheetPlugin
    {
        [KernelFunction(nameof(AddSheet)), Description("Add a new sheet to an Excel workbook.")]
        public string AddSheet([Description("Local file path"), Required] string filePath, [Description("Worksheet name"), Required] string sheetName)
        {
            var wb = ExcelHelper.GetOrOpenWorkbook(filePath);
            if (wb == null) return $"Workbook '{filePath}' could not be opened.";

            Worksheet newSheet = (Worksheet)wb.Sheets.Add();
            newSheet.Name = sheetName;

            return $"Sheet '{sheetName}' added to workbook '{wb.Name}'.";
        }

        [KernelFunction(nameof(DeleteSheet)), Description("Delete worksheet with provided name.")]
        public string DeleteSheet([Description("Local file path"), Required] string filePath, [Description("Worksheet name"), Required] string sheetName)
        {
            var wb = ExcelHelper.GetOrOpenWorkbook(filePath);
            if (wb == null) return $"Workbook '{filePath}' could not be opened.";

            Worksheet ws = null;
            foreach (Worksheet sheet in wb.Sheets)
            {
                if (sheet.Name == sheetName)
                {
                    ws = sheet;
                    break;
                }
            }

            if (ws == null) return $"Sheet '{sheetName}' not found in workbook '{wb.Name}'.";

            ws.Delete();
            return $"Sheet '{sheetName}' deleted from workbook '{wb.Name}'.";
        }

        [KernelFunction(nameof(RenameSheet)), Description("Find and Rename a worksheet with provided name and new name.")]
        public string RenameSheet([Description("Local file path"), Required] string filePath, [Description("Old Worksheet name"), Required] string oldName, [Description("New Worksheet name"), Required] string newName)
        {
            var wb = ExcelHelper.GetOrOpenWorkbook(filePath);
            if (wb == null) return $"Workbook '{filePath}' could not be opened.";

            Worksheet ws = null;
            foreach (Worksheet sheet in wb.Sheets)
            {
                if (sheet.Name == oldName)
                {
                    ws = sheet;
                    break;
                }
            }

            if (ws == null) return $"Sheet '{oldName}' not found in workbook '{wb.Name}'.";

            ws.Name = newName;
            return $"Sheet renamed from '{oldName}' to '{newName}' in workbook '{wb.Name}'.";
        }

        [KernelFunction(nameof(ListSheets)), Description("List all worksheets of a excel file")]
        public string ListSheets([Description("Local file path"), Required] string filePath)
        {
            var wb = ExcelHelper.GetOrOpenWorkbook(filePath);
            if (wb == null) return $"Workbook '{filePath}' could not be opened.";

            var sheetNames = "";
            foreach (Worksheet sheet in wb.Sheets)
            {
                sheetNames += sheet.Name + ", ";
            }

            return string.IsNullOrEmpty(sheetNames) ? "No sheets found." : sheetNames.TrimEnd(',', ' ');
        }
    }
}

