using DeskMind.Plugins.Office.Helpers;

using Microsoft.Office.Interop.PowerPoint;
using Microsoft.SemanticKernel;

using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace DeskMind.Plugins.Office.PowerPoint
{
    public class PowerPointSlidePlugin
    {
        private Application GetPowerPointApp() =>
            (Application)Marshal2.GetActiveObject("PowerPoint.Application");

        [KernelFunction, Description("Add a new slide at the end of the presentation.")]
        public string AddSlide([Description("File path"), Required] string filePath,
                               [Description("Slide layout index"), Required] int layout = 1)
        {
            var app = GetPowerPointApp();
            var pres = app.Presentations.Cast<Presentation>().FirstOrDefault(p => p.FullName == filePath);
            if (pres == null) return $"Presentation '{filePath}' not found.";

            pres.Slides.Add(pres.Slides.Count + 1, (PpSlideLayout)layout);
            return $"Added a new slide to '{filePath}'.";
        }

        [KernelFunction, Description("Delete a slide by index.")]
        public string DeleteSlide([Description("File path"), Required] string filePath,
                                  [Description("Slide index"), Required] int slideIndex)
        {
            var app = GetPowerPointApp();
            var pres = app.Presentations.Cast<Presentation>().FirstOrDefault(p => p.FullName == filePath);
            if (pres == null) return $"Presentation '{filePath}' not found.";

            if (slideIndex < 1 || slideIndex > pres.Slides.Count)
                return $"Invalid slide index {slideIndex}.";

            pres.Slides[slideIndex].Delete();
            return $"Deleted slide {slideIndex} from '{filePath}'.";
        }

        [KernelFunction, Description("Update text in a specific slide shape.")]
        public string UpdateSlideText([Description("File path"), Required] string filePath,
                                      [Description("Slide index"), Required] int slideIndex,
                                      [Description("Shape name"), Required] string shapeName,
                                      [Description("New text"), Required] string text)
        {
            var app = GetPowerPointApp();
            var pres = app.Presentations.Cast<Presentation>().FirstOrDefault(p => p.FullName == filePath);
            if (pres == null) return $"Presentation '{filePath}' not found.";

            var slide = pres.Slides[slideIndex];
            var shape = slide.Shapes.Cast<Shape>().FirstOrDefault(s => s.Name == shapeName);
            if (shape == null) return $"Shape '{shapeName}' not found on slide {slideIndex}.";

            shape.TextFrame.TextRange.Text = text;
            return $"Updated text in shape '{shapeName}' on slide {slideIndex}.";
        }
    }
}

