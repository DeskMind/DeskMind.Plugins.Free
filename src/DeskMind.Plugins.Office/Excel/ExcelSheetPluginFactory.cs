using DeskMind.Core.Plugins;
using DeskMind.Plugins.Office.Helpers;
using Microsoft.SemanticKernel;

namespace DeskMind.Plugins.Office.Excel
{
    public class ExcelSheetPluginFactory : SecurePluginFactoryBase
    {
        public override PluginMetadata Metadata => new PluginMetadata
        {
            Name = "Excel Sheet Plugin",
            Version = "1.0.0",
            Description = "Sheet-level operations for Excel.",
            Dependencies = new[] { "COM Excel Installed" }
        };

        public override bool IsAvailable() => GeneralHelpers.IsOfficeInstalled("Excel");

        protected override KernelPlugin CreatePluginCore() => 
            KernelPluginFactory.CreateFromType<ExcelSheetPlugin>();
    }
}

