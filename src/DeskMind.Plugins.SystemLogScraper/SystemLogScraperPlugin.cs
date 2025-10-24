using DeskMind.Plugins.SystemLogScraper.Models;
using DeskMind.Plugins.SystemLogScraper.Services;
using DeskMind.Plugins.WebScraper;

using Microsoft.SemanticKernel;

using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace DeskMind.Plugins.SystemLogScraper
{
    public class SystemLogScraperPlugin
    {
        private readonly SystemRegistry _registry;
        private readonly WebScraperPlugin _webScraper;

        /// <summary>
        /// Constructor accepts configuration paths.
        /// </summary>
        /// <param name="systemRegistryPath">Path to persisted systems XML</param>
        /// <param name="webScraperRulesFolder">Folder containing rules XML files</param>
        public SystemLogScraperPlugin(string systemRegistryPath = null, string webScraperRulesFolder = null)
        {
            _registry = new SystemRegistry();
            if (!string.IsNullOrEmpty(systemRegistryPath))
            {
                _registry.LoadFromXml(systemRegistryPath);
            }

            // Pass the rules folder path to WebScraperPlugin
            _webScraper = new WebScraperPlugin(webScraperRulesFolder ?? string.Empty);
        }

        [KernelFunction, Description("Returns all available sites for a given system and the data they contain.")]
        public IEnumerable<SystemSite> ListSites(
            [Description("System name"), Required] string systemName)
        {
            var system = _registry.GetSystem(systemName);
            return system?.GetAvailableSites() ?? new List<SystemSite>();
        }

        [KernelFunction, Description("Scrapes data from the given system site.")]
        public async Task<string> ScrapeSite(
            [Description("System name"), Required] string systemName,
            [Description("Site name"), Required] string siteName)
        {
            var system = _registry.GetSystem(systemName);
            if (system == null)
                return $"System '{systemName}' not found.";

            var site = system.Sites.FirstOrDefault(s => s.Name.Equals(siteName, System.StringComparison.OrdinalIgnoreCase));
            if (site == null)
                return $"Site '{siteName}' not found for system '{systemName}'.";

            var url = system.GetSiteUrl(siteName);

            // Use rules file if defined in site configuration
            if (!string.IsNullOrEmpty(site.RulesFile))
            {
                return await _webScraper.ScrapePage(url, site.RulesFile);
            }

            // Fallback: extract tables if no rules
            return await _webScraper.ExtractTables(url);
        }

        [KernelFunction, Description("Checks if a configuration exists for a system site.")]
        public bool ConfigExists(
            [Description("System name"), Required] string systemName,
            [Description("Site name"), Required] string siteName)
        {
            var system = _registry.GetSystem(systemName);
            return system?.Sites.Any(s => s.Name.Equals(siteName, System.StringComparison.OrdinalIgnoreCase)) ?? false;
        }
    }
}

