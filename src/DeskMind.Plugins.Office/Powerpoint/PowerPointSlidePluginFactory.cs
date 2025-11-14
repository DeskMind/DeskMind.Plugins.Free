using DeskMind.Core.Plugins;
using DeskMind.Plugins.Office.Helpers;
using Microsoft.SemanticKernel;
using DeskMind.Plugins.Office.Resources;

namespace DeskMind.Plugins.Office.PowerPoint
{
    public class PowerPointSlidePluginFactory : SecurePluginFactoryBase
    {
        public override PluginMetadata Metadata => new PluginMetadata
        {
            Name = PluginStrings.PowerPointSlide_Name,
            Version = "1.0.0",
            Description = PluginStrings.PowerPointSlide_Description,
            Dependencies = new[] { PluginStrings.PowerPointSlide_Dependency_Com }
        };

        public override bool IsAvailable() => GeneralHelpers.IsOfficeInstalled("PowerPoint");

        protected override KernelPlugin CreatePluginCore() =>
            KernelPluginFactory.CreateFromType<PowerPointSlidePlugin>();
    }
}

