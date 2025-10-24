using DeskMind.Core.Plugins;

using Microsoft.SemanticKernel;

using System.Collections.Generic;

namespace DeskMind.Plugins.WebSearch.Duck
{
    public class WebScraperPluginFactory : SecurePluginFactoryBase
    {
        public override PluginMetadata Metadata => new PluginMetadata
        {
            Name = "Web Search Plugin",
            Version = "1.0.0",
            Description = "Search the web and return links and their respective information using DuckDuckGo.",
        };

        public override bool IsAvailable()
        {
            // WebScraper only needs .NET + libraries, so always true
            return true;
        }

        protected override KernelPlugin CreatePluginCore()
        {
            return KernelPluginFactory.CreateFromType(typeof(WebSearchPlugin));
        }

        protected override List<PluginConfig> GetDefaultConfigurations()
        {
            return new List<PluginConfig>
            {
            };
        }
    }
}