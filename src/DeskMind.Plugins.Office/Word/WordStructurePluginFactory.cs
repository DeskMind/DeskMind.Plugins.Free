using DeskMind.Core.Plugins;
using DeskMind.Plugins.Office.Helpers;
using Microsoft.SemanticKernel;

namespace DeskMind.Plugins.Office.Word
{
    public class WordStructurePluginFactory : SecurePluginFactoryBase
    {
        public override PluginMetadata Metadata => new PluginMetadata
        {
            Name = "Word Structure Plugin",
            Version = "1.0.0",
            Description = "Structure-level operations for Word.",
            Dependencies = new[] { "COM Word Installed" }
        };

        public override bool IsAvailable() => GeneralHelpers.IsOfficeInstalled("Word");

        protected override KernelPlugin CreatePluginCore() => 
            KernelPluginFactory.CreateFromType<WordStructurePlugin>();
    }
}

