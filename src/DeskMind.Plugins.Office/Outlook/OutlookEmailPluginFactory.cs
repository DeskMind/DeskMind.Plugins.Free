using DeskMind.Core.Plugins;
using DeskMind.Plugins.Office.Helpers;
using Microsoft.SemanticKernel;

namespace DeskMind.Plugins.Office.Outlook
{
    public class OutlookEmailPluginFactory : SecurePluginFactoryBase
    {
        public override PluginMetadata Metadata => new PluginMetadata
        {
            Name = "Outlook Email Plugin",
            Version = "1.0.0",
            Description = "Email operations for Outlook.",
            Dependencies = new[] { "COM Outlook Installed" }
        };

        public override bool IsAvailable() => GeneralHelpers.IsOfficeInstalled("Outlook");

        protected override KernelPlugin CreatePluginCore() => 
            KernelPluginFactory.CreateFromType<OutlookEmailPlugin>();
    }
}

