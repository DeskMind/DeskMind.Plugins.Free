using DeskMind.Core.Plugins;
using DeskMind.Plugins.Office.Helpers;
using Microsoft.SemanticKernel;
using DeskMind.Plugins.Office.Resources;

namespace DeskMind.Plugins.Office.PowerPoint
{
    public class PowerPointPresentationPluginFactory : SecurePluginFactoryBase
    {
        public override PluginMetadata Metadata => new PluginMetadata
        {
            Name = PluginStrings.PowerPointPresentation_Name,
            Version = "1.0.0",
            Description = PluginStrings.PowerPointPresentation_Description,
            Dependencies = new[] { PluginStrings.PowerPointPresentation_Dependency_Com }
        };

        public override bool IsAvailable() => GeneralHelpers.IsOfficeInstalled("PowerPoint");

        protected override KernelPlugin CreatePluginCore() =>
            KernelPluginFactory.CreateFromType<PowerPointPresentationPlugin>();
    }
}

