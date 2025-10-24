using DeskMind.Plugins.Office.Helpers;

using Microsoft.Office.Interop.Word;
using Microsoft.SemanticKernel;

using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace DeskMind.Plugins.Office.Word
{
    public class WordTextPlugin
    {
        private Application GetWordApp() =>
            (Application)Marshal2.GetActiveObject("Word.Application");

        [KernelFunction, Description("Find and replace text in a Word document.")]
        public string FindAndReplace(
            [Description("File path of the Word document"), Required] string filePath,
            [Description("Text to find"), Required] string findText,
            [Description("Text to replace with"), Required] string replaceText)
        {
            var app = GetWordApp();
            var doc = app.Documents.Open(filePath);

            var range = doc.Content;
            range.Find.Execute(FindText: findText, ReplaceWith: replaceText, Replace: WdReplace.wdReplaceAll);

            doc.Save();
            doc.Close();
            return $"Replaced all '{findText}' with '{replaceText}' in {filePath}.";
        }

        [KernelFunction, Description("Insert text at the end of a Word document.")]
        public string InsertTextAtEnd(
            [Description("File path of the Word document"), Required] string filePath,
            [Description("Text to insert"), Required] string text)
        {
            var app = GetWordApp();
            var doc = app.Documents.Open(filePath);

            doc.Content.InsertAfter(text);
            doc.Save();
            doc.Close();

            return $"Inserted text at the end of {filePath}.";
        }

        [KernelFunction, Description("Format the font of a selection or range.")]
        public string FormatText(
            [Description("File path of the Word document"), Required] string filePath,
            [Description("Text to format (exact match)"), Required] string targetText,
            [Description("Bold true/false"), Required] bool bold,
            [Description("Italic true/false"), Required] bool italic)
        {
            var app = GetWordApp();
            var doc = app.Documents.Open(filePath);

            var range = doc.Content;
            if (range.Find.Execute(FindText: targetText))
            {
                range.Font.Bold = bold ? 1 : 0;
                range.Font.Italic = italic ? 1 : 0;
            }

            doc.Save();
            doc.Close();

            return $"Formatted text '{targetText}' in {filePath} (Bold={bold}, Italic={italic}).";
        }

        [KernelFunction, Description("Get all text from a Word document.")]
        public string GetDocumentText(
            [Description("File path of the Word document"), Required] string filePath)
        {
            var app = GetWordApp();
            var doc = app.Documents.Open(filePath);

            string text = doc.Content.Text;
            doc.Close();

            return text;
        }
    }
}

