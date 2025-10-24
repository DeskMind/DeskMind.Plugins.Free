using DeskMind.Core.Plugins;
using DeskMind.Plugins.Office.Helpers;

using Microsoft.SemanticKernel;

using System;

namespace DeskMind.Plugins.Office.Excel
{
    public class ExcelCellPluginFactory : SecurePluginFactoryBase
    {
        public override PluginMetadata Metadata => new PluginMetadata
        {
            Name = "Excel Cell Plugin",
            Version = "1.0.0",
            Description = "Cell-level operations for Excel.",
            Dependencies = new[] { "COM Excel Installed" },
            RequiredRoles = new[] { "Admin" },
        };

        public override bool IsAvailable() => GeneralHelpers.IsOfficeInstalled("Excel");

        protected override KernelPlugin CreatePluginCore() =>
            KernelPluginFactory.CreateFromType<ExcelCellPlugin>();
    }
}

