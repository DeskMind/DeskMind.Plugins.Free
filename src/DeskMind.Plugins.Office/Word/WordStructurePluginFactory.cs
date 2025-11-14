using DeskMind.Core.Plugins;
using DeskMind.Plugins.Office.Helpers;
using Microsoft.SemanticKernel;
using DeskMind.Plugins.Office.Resources;

namespace DeskMind.Plugins.Office.Word
{
    public class WordStructurePluginFactory : SecurePluginFactoryBase
    {
        public override PluginMetadata Metadata => new PluginMetadata
        {
            Name = PluginStrings.WordStructure_Name,
            Version = "1.0.0",
            Description = PluginStrings.WordStructure_Description,
            Dependencies = new[] { PluginStrings.WordStructure_Dependency_Com }
        };

        public override bool IsAvailable() => GeneralHelpers.IsOfficeInstalled("Word");

        protected override KernelPlugin CreatePluginCore() => 
            KernelPluginFactory.CreateFromType<WordStructurePlugin>();
    }
}

