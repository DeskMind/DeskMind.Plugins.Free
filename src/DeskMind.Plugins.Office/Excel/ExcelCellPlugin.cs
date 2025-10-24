using Microsoft.Office.Interop.Excel;
using Microsoft.SemanticKernel;

using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace DeskMind.Plugins.Office.Excel
{
    public class ExcelCellPlugin
    {
        [KernelFunction(nameof(ReplaceCell)), Description("Replace the value of a specific cell in the given workbook and sheet.")]
        public string ReplaceCell(
            [Description("Local file path"), Required] string filePath,
            [Description("Worksheet name"), Required] string sheetName,
            [Description("Cell reference (e.g. A1)"), Required] string cellRef,
            [Description("New value"), Required] string newValue)
        {
            var wb = ExcelHelper.GetOrOpenWorkbook(filePath);
            if (wb == null) return $"Workbook '{filePath}' could not be opened.";

            var ws = wb.Sheets[sheetName] as Worksheet;
            if (ws == null) return $"Sheet '{sheetName}' not found in workbook '{wb.Name}'.";

            ws.Range[cellRef].Value2 = newValue;
            return $"Cell '{cellRef}' updated to '{newValue}' in workbook '{wb.Name}'.";
        }

        [KernelFunction(nameof(FormatCellFont)), Description("Format the font of a specific cell (bold, italic).")]
        public string FormatCellFont(
            [Description("Local file path"), Required] string filePath,
            [Description("Worksheet name"), Required] string sheetName,
            [Description("Cell reference (e.g. A1)"), Required] string cellRef,
            [Description("Bold"), Required] bool bold,
            [Description("Italic"), Required] bool italic)
        {
            var wb = ExcelHelper.GetOrOpenWorkbook(filePath);
            if (wb == null) return $"Workbook '{filePath}' could not be opened.";

            var ws = wb.Sheets[sheetName] as Worksheet;
            if (ws == null) return $"Sheet '{sheetName}' not found in workbook '{wb.Name}'.";

            var cell = ws.Range[cellRef];
            cell.Font.Bold = bold;
            cell.Font.Italic = italic;

            return $"Cell '{cellRef}' font updated (Bold={bold}, Italic={italic}) in workbook '{wb.Name}'.";
        }

        [KernelFunction(nameof(FormatCellColor)), Description("Format the background color of a specific cell.")]
        public string FormatCellColor(
            [Description("Local file path"), Required] string filePath,
            [Description("Worksheet name"), Required] string sheetName,
            [Description("Cell reference (e.g. A1)"), Required] string cellRef,
            [Description("RGB color value"), Required] int rgb)
        {
            var wb = ExcelHelper.GetOrOpenWorkbook(filePath);
            if (wb == null) return $"Workbook '{filePath}' could not be opened.";

            var ws = wb.Sheets[sheetName] as Worksheet;
            if (ws == null) return $"Sheet '{sheetName}' not found in workbook '{wb.Name}'.";

            ws.Range[cellRef].Interior.Color = rgb;
            return $"Cell '{cellRef}' color updated in workbook '{wb.Name}'.";
        }

        [KernelFunction(nameof(GetCellValue)), Description("Get the value of a specific cell in the given workbook and sheet.")]
        public string GetCellValue(
            [Description("Local file path"), Required] string filePath,
            [Description("Worksheet name"), Required] string sheetName,
            [Description("Cell reference (e.g. A1)"), Required] string cellRef)
        {
            var wb = ExcelHelper.GetOrOpenWorkbook(filePath);
            if (wb == null) return $"Workbook '{filePath}' could not be opened.";
            var ws = wb.Sheets[sheetName] as Worksheet;
            if (ws == null) return $"Sheet '{sheetName}' not found in workbook '{wb.Name}'.";
            var value = ws.Range[cellRef].Value2;
            return value?.ToString() ?? "";
        }

        [KernelFunction(nameof(GetUsedRange)), Description("Get the used range of a worksheet as a markdown table.")]
        public string GetUsedRange(
            [Description("Local file path"), Required] string filePath,
            [Description("Worksheet name"), Required] string sheetName)
        {
            var wb = ExcelHelper.GetOrOpenWorkbook(filePath);
            if (wb == null) return $"Workbook '{filePath}' could not be opened.";
            var ws = wb.Sheets[sheetName] as Worksheet;
            if (ws == null) return $"Sheet '{sheetName}' not found in workbook '{wb.Name}'.";
            var usedRange = ws.UsedRange;
            var rows = usedRange.Rows.Count;
            var cols = usedRange.Columns.Count;
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("| " + string.Join(" | ", Enumerable.Range(1, cols).Select(i => ws.Cells[1, i].Value2?.ToString() ?? "")) + " |");
            sb.AppendLine("|" + string.Join("|", Enumerable.Repeat("---", cols)) + "|");
            for (int r = 2; r <= rows; r++)
            {
                sb.Append("| ");
                for (int c = 1; c <= cols; c++)
                {
                    sb.Append((ws.Cells[r, c].Value2?.ToString() ?? "") + " | ");
                }
                sb.AppendLine();
            }
            return sb.ToString();
        }

        [KernelFunction(nameof(FindAndReplaceByContent)), Description("Find cells by content and replace with provided value.")]
        public string FindAndReplaceByContent(
            [Description("Local file path"), Required] string filePath,
            [Description("Worksheet name"), Required] string sheetName,
            [Description("Content to find"), Required] string findValue,
            [Description("Replacement value"), Required] string replaceValue)
        {
            var wb = ExcelHelper.GetOrOpenWorkbook(filePath);
            if (wb == null) return $"Workbook '{filePath}' could not be opened.";
            var ws = wb.Sheets[sheetName] as Worksheet;
            if (ws == null) return $"Sheet '{sheetName}' not found in workbook '{wb.Name}'.";
            var usedRange = ws.UsedRange;
            int count = 0;
            foreach (Range cell in usedRange.Cells)
            {
                if ((cell.Value2?.ToString() ?? "") == findValue)
                {
                    cell.Value2 = replaceValue;
                    count++;
                }
            }
            return $"Replaced {count} cell(s) containing '{findValue}' with '{replaceValue}' in worksheet '{sheetName}'.";
        }
    }
}

