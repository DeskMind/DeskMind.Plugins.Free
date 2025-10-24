using DeskMind.Core.Plugins;
using DeskMind.Plugins.Office.Helpers;
using Microsoft.SemanticKernel;

namespace DeskMind.Plugins.Office.PowerPoint
{
    public class PowerPointPresentationPluginFactory : SecurePluginFactoryBase
    {
        public override PluginMetadata Metadata => new PluginMetadata
        {
            Name = "PowerPoint Presentation Plugin",
            Version = "1.0.0",
            Description = "Presentation-level operations for PowerPoint.",
            Dependencies = new[] { "COM PowerPoint Installed" }
        };

        public override bool IsAvailable() => GeneralHelpers.IsOfficeInstalled("PowerPoint");

        protected override KernelPlugin CreatePluginCore() =>
            KernelPluginFactory.CreateFromType<PowerPointPresentationPlugin>();
    }
}

