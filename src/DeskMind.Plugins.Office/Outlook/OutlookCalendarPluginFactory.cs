using DeskMind.Core.Plugins;
using DeskMind.Plugins.Office.Helpers;
using Microsoft.SemanticKernel;

namespace DeskMind.Plugins.Office.Outlook
{
    public class OutlookCalendarPluginFactory : SecurePluginFactoryBase
    {
        public override PluginMetadata Metadata => new PluginMetadata
        {
            Name = "Outlook Calendar Plugin",
            Version = "1.0.0",
            Description = "Calendar operations for Outlook.",
            Dependencies = new[] { "COM Outlook Installed" }
        };

        public override bool IsAvailable() => GeneralHelpers.IsOfficeInstalled("Outlook");

        protected override KernelPlugin CreatePluginCore() => KernelPluginFactory.CreateFromType<OutlookCalendarPlugin>();
    }
}

