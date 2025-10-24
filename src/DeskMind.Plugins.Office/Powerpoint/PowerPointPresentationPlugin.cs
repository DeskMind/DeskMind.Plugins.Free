using DeskMind.Plugins.Office.Helpers;
using Microsoft.Office.Interop.PowerPoint;
using Microsoft.SemanticKernel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using Microsoft.Office.Core; // Add reference for MsoTriState

namespace DeskMind.Plugins.Office.PowerPoint
{
    public class PowerPointPresentationPlugin
    {
        private Application GetPowerPointApp() =>
            (Application)Marshal2.GetActiveObject("PowerPoint.Application");

        [KernelFunction, Description("Open a PowerPoint presentation.")]
        public string OpenPresentation([Description("File path"), Required] string filePath)
        {
            var app = GetPowerPointApp();
            var pres = app.Presentations.Open(filePath);
            return $"Presentation '{filePath}' opened.";
        }

        [KernelFunction, Description("Save the PowerPoint presentation.")]
        public string SavePresentation([Description("File path"), Required] string filePath)
        {
            var app = GetPowerPointApp();
            var pres = app.Presentations.Cast<Presentation>().FirstOrDefault(p => p.FullName == filePath);
            if (pres == null) return $"Presentation '{filePath}' not found.";
            pres.Save();
            return $"Presentation '{filePath}' saved.";
        }

        [KernelFunction, Description("Export presentation to PDF.")]
        public string ExportToPdf([Description("File path"), Required] string filePath,
                                  [Description("PDF output path"), Required] string pdfPath)
        {
            var app = GetPowerPointApp();
            var pres = app.Presentations.Cast<Presentation>().FirstOrDefault(p => p.FullName == filePath);
            if (pres == null) return $"Presentation '{filePath}' not found.";

            pres.ExportAsFixedFormat(pdfPath, PpFixedFormatType.ppFixedFormatTypePDF);
            return $"Presentation '{filePath}' exported to PDF at '{pdfPath}'.";
        }

        [KernelFunction, Description("List all slide titles in the presentation.")]
        public string ListSlides([Description("File path"), Required] string filePath)
        {
            var app = GetPowerPointApp();
            var pres = app.Presentations.Cast<Presentation>().FirstOrDefault(p => p.FullName == filePath);
            if (pres == null) return $"Presentation '{filePath}' not found.";

            var sb = new StringBuilder();
            foreach (Slide slide in pres.Slides)
            {
                string title = "<No title>";
                try
                {
                    if (slide.Shapes.HasTitle == MsoTriState.msoTrue && slide.Shapes.Title != null)
                        title = slide.Shapes.Title.TextFrame?.TextRange?.Text ?? "<No title>";
                }
                catch { /* ignore and use default title */ }
                sb.AppendLine($"{slide.SlideIndex}: {title}");
            }
            return sb.ToString();
        }
    }
}

