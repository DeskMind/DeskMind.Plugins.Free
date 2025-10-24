using DeskMind.Core.Plugins;
using DeskMind.Plugins.Office.Helpers;
using Microsoft.SemanticKernel;

namespace DeskMind.Plugins.Office.PowerPoint
{
    public class PowerPointSlidePluginFactory : SecurePluginFactoryBase
    {
        public override PluginMetadata Metadata => new PluginMetadata
        {
            Name = "PowerPoint Slide Plugin",
            Version = "1.0.0",
            Description = "Slide-level operations for PowerPoint.",
            Dependencies = new[] { "COM PowerPoint Installed" }
        };

        public override bool IsAvailable() => GeneralHelpers.IsOfficeInstalled("PowerPoint");

        protected override KernelPlugin CreatePluginCore() =>
            KernelPluginFactory.CreateFromType<PowerPointSlidePlugin>();
    }
}

