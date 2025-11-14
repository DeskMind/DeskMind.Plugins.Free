using DeskMind.Core.Plugins;
using DeskMind.Plugins.Office.Helpers;

using Microsoft.SemanticKernel;

using System;
using DeskMind.Plugins.Office.Resources;

namespace DeskMind.Plugins.Office.Excel
{
    public class ExcelCellPluginFactory : SecurePluginFactoryBase
    {
        public override PluginMetadata Metadata => new PluginMetadata
        {
            Name = PluginStrings.ExcelCell_Name,
            Version = "1.0.0",
            Description = PluginStrings.ExcelCell_Description,
            Dependencies = new[] { PluginStrings.ExcelCell_Dependency_Com },
            RequiredRoles = new[] { PluginStrings.Common_Role_Admin },
        };

        public override bool IsAvailable() => GeneralHelpers.IsOfficeInstalled("Excel");

        protected override KernelPlugin CreatePluginCore() =>
            KernelPluginFactory.CreateFromType<ExcelCellPlugin>();
    }
}

