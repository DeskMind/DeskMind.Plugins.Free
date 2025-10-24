using DeskMind.Core.Plugins;

using Microsoft.SemanticKernel;

using System;
using System.Collections.Generic;
using System.IO;

namespace DeskMind.Plugins.WebScraper
{
    public class WebScraperPluginFactory : SecurePluginFactoryBase
    {
        public override PluginMetadata Metadata => new PluginMetadata
        {
            Name = "Web Scraper Plugin",
            Version = "1.0.0",
            Description = "Scrapes static and dynamic web pages using XML-based extraction rules.",
            Dependencies = new[] { "HtmlAgilityPack", "Playwright" }
        };

        public override bool IsAvailable()
        {
            // WebScraper only needs .NET + libraries, so always true
            return true;
        }

        protected override KernelPlugin CreatePluginCore()
        {
            var rulesFolder = GetConfigValue<string>("RulesFolder", Path.Combine(AppContext.BaseDirectory, "rules"));
            var timeout = GetConfigValue<int>("TimeoutSeconds", 30);

            var scraper = new WebScraperPlugin(rulesFolder);

            return KernelPluginFactory.CreateFromObject(scraper);
        }

        protected override List<PluginConfig> GetDefaultConfigurations()
        {
            return new List<PluginConfig>
            {
               new PluginConfig ("RulesFolder", typeof(string).AssemblyQualifiedName, Path.Combine(AppContext.BaseDirectory, "rules")),
                 new PluginConfig("TimeoutSeconds", typeof(int).AssemblyQualifiedName, "30")
            };
        }
    }
}

