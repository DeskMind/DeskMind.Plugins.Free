using DeskMind.Plugins.Office.Helpers;

using Microsoft.Office.Interop.Word;
using Microsoft.SemanticKernel;

using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace DeskMind.Plugins.Office.Word
{
    public class WordStructurePlugin
    {
        private Application GetWordApp() =>
            (Application)Marshal2.GetActiveObject("Word.Application");

        [KernelFunction, Description("Insert a heading into the Word document.")]
        public string AddHeading(
            [Description("File path of the Word document"), Required] string filePath,
            [Description("Heading text"), Required] string headingText,
            [Description("Heading level (1-6)"), Required] int level)
        {
            var app = GetWordApp();
            var doc = app.Documents.Open(filePath);

            var range = doc.Content;
            range.Collapse(WdCollapseDirection.wdCollapseEnd);
            range.Text = headingText;
            range.set_Style($"Heading {level}");

            doc.Save();
            doc.Close();

            return $"Added Heading {level}: '{headingText}' to {filePath}.";
        }

        [KernelFunction, Description("Insert a table at the end of a Word document.")]
        public string AddTable(
            [Description("File path of the Word document"), Required] string filePath,
            [Description("Number of rows"), Required] int rows,
            [Description("Number of columns"), Required] int cols)
        {
            var app = GetWordApp();
            var doc = app.Documents.Open(filePath);

            var range = doc.Content;
            range.Collapse(WdCollapseDirection.wdCollapseEnd);
            doc.Tables.Add(range, rows, cols);

            doc.Save();
            doc.Close();

            return $"Inserted {rows}x{cols} table into {filePath}.";
        }

        [KernelFunction, Description("List all headings in the document.")]
        public string ListHeadings(
            [Description("File path of the Word document"), Required] string filePath)
        {
            var app = GetWordApp();
            var doc = app.Documents.Open(filePath);

            var sb = new StringBuilder();
            foreach (Paragraph para in doc.Paragraphs)
            {
                if (para.get_Style() is Style style && style.NameLocal.StartsWith("Heading"))
                {
                    sb.AppendLine($"{style.NameLocal}: {para.Range.Text.Trim()}");
                }
            }

            doc.Close();
            return sb.Length > 0 ? sb.ToString() : "No headings found.";
        }

        [KernelFunction, Description("Export Word document to PDF.")]
        public string ExportToPdf(
            [Description("File path of the Word document"), Required] string filePath,
            [Description("PDF output file path"), Required] string pdfPath)
        {
            var app = GetWordApp();
            var doc = app.Documents.Open(filePath);

            doc.ExportAsFixedFormat(pdfPath, WdExportFormat.wdExportFormatPDF);
            doc.Close();

            return $"Exported {filePath} to {pdfPath}.";
        }
    }
}

