using DeskMind.Core.Plugins;
using DeskMind.Plugins.Office.Helpers;
using Microsoft.SemanticKernel;

namespace DeskMind.Plugins.Office.Word
{
    public class WordTextPluginFactory : SecurePluginFactoryBase
    {
        public override PluginMetadata Metadata => new PluginMetadata
        {
            Name = "Word Text Plugin",
            Version = "1.0.0",
            Description = "Text-level operations for Word.",
            Dependencies = new[] { "COM Word Installed" }
        };

        public override bool IsAvailable() => GeneralHelpers.IsOfficeInstalled("Word");

        protected override KernelPlugin CreatePluginCore() => 
            KernelPluginFactory.CreateFromType<WordTextPlugin>();
    }
}

