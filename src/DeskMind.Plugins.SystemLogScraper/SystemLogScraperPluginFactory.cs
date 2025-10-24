using DeskMind.Core.Plugins;

using Microsoft.SemanticKernel;

using System;
using System.Collections.Generic;
using System.IO;

namespace DeskMind.Plugins.SystemLogScraper
{
    public class SystemLogScraperPluginFactory : SecurePluginFactoryBase
    {
        public override PluginMetadata Metadata => new PluginMetadata
        {
            Name = "System Log Scraper Plugin",
            Version = "1.0.0",
            Description = "Scrapes log data from registered systems and sites, using WebScraper as backend.",
            Dependencies = new[] { "HtmlAgilityPack", "Playwright" }
        };

        public override bool IsAvailable()
        {
            // Requires .NET + WebScraper dependencies
            return true;
        }

        protected override KernelPlugin CreatePluginCore()
        {
            // Use helper so defaults are guaranteed
            var systemRegistryPath = GetConfigValue("SystemRegistryPath", Path.Combine(AppContext.BaseDirectory, "systems.xml"));
            var webScraperRulesFolder = GetConfigValue("WebScraperRulesFolder", Path.Combine(AppContext.BaseDirectory, "rules"));

            var plugin = new SystemLogScraperPlugin(systemRegistryPath, webScraperRulesFolder);
            return KernelPluginFactory.CreateFromObject(plugin);
        }

        /// <summary>
        /// Provide defaults if no config file is present.
        /// </summary>
        protected override List<PluginConfig> GetDefaultConfigurations()
        {
            return new List<PluginConfig>
            {
                new PluginConfig ("SystemRegistryPath", typeof(string).FullName ?? "string", Path.Combine(AppContext.BaseDirectory, "systems.xml")),
                new PluginConfig ("WebScraperRulesFolder", typeof(string).FullName ?? "string", Path.Combine(AppContext.BaseDirectory, "rules"))
            };
        }
    }
}

