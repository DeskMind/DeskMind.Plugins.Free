using DeskMind.Core.Plugins;
using DeskMind.Plugins.Office.Helpers;
using Microsoft.SemanticKernel;
using DeskMind.Plugins.Office.Resources;

namespace DeskMind.Plugins.Office.Excel
{
    public class ExcelSheetPluginFactory : SecurePluginFactoryBase
    {
        public override PluginMetadata Metadata => new PluginMetadata
        {
            Name = PluginStrings.ExcelSheet_Name,
            Version = "1.0.0",
            Description = PluginStrings.ExcelSheet_Description,
            Dependencies = new[] { PluginStrings.ExcelSheet_Dependency_Com }
        };

        public override bool IsAvailable() => GeneralHelpers.IsOfficeInstalled("Excel");

        protected override KernelPlugin CreatePluginCore() => 
            KernelPluginFactory.CreateFromType<ExcelSheetPlugin>();
    }
}

