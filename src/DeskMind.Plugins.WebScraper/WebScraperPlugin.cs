using HtmlAgilityPack;

using Microsoft.SemanticKernel;

using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace DeskMind.Plugins.WebScraper
{
    public class WebScraperPlugin
    {
        private readonly string _rulesFolder;

        public WebScraperPlugin(string rulesFolder)
        {
            _rulesFolder = rulesFolder;
        }

        private async Task<string> LoadHtmlAsync(string url)
        {
            using var http = new HttpClient();
            return await http.GetStringAsync(url);
        }

        private string GetRulesFilePath(string rulesFileName)
        {
            if (Path.IsPathRooted(rulesFileName))
                return rulesFileName; // full path provided
            return Path.Combine(_rulesFolder, rulesFileName);
        }

        [KernelFunction, Description("Scrape data from a web page based on XML extraction rules.")]
        public async Task<string> ScrapePage(
            [Description("URL of the target page"), Required] string url,
            [Description("XML rules file name"), Required] string rulesFileName)
        {
            var rulesFilePath = GetRulesFilePath(rulesFileName);
            if (!File.Exists(rulesFilePath))
                return $"Rules file not found: {rulesFilePath}";

            var html = await LoadHtmlAsync(url);
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var rules = XDocument.Load(rulesFilePath);
            var sb = new StringBuilder();

            foreach (var field in rules.Root.Elements("Field"))
            {
                var name = field.Attribute("name")?.Value ?? "Unnamed";
                var xpath = field.Attribute("xpath")?.Value;
                if (string.IsNullOrWhiteSpace(xpath)) continue;

                var nodes = doc.DocumentNode.SelectNodes(xpath);
                if (nodes != null)
                    sb.AppendLine($"{name}: {string.Join(", ", nodes.Select(n => n.InnerText.Trim()))}");
                else
                    sb.AppendLine($"{name}: [Not found]");
            }

            return sb.ToString();
        }

        [KernelFunction, Description("Extract all links from a web page.")]
        public async Task<string> ExtractLinks(
            [Description("URL of the target page"), Required] string url)
        {
            var html = await LoadHtmlAsync(url);
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var links = doc.DocumentNode.SelectNodes("//a[@href]")
                ?.Select(a => a.GetAttributeValue("href", string.Empty))
                .Where(href => !string.IsNullOrWhiteSpace(href))
                .Distinct()
                .ToList();

            if (links == null || links.Count == 0)
                return "No links found.";

            var sb = new StringBuilder();
            foreach (var link in links)
                sb.AppendLine(link);

            return sb.ToString();
        }

        [KernelFunction, Description("Extract all tables from a web page and return them as markdown.")]
        public async Task<string> ExtractTables(
            [Description("URL of the target page"), Required] string url)
        {
            var html = await LoadHtmlAsync(url);
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var tables = doc.DocumentNode.SelectNodes("//table");
            if (tables == null || tables.Count == 0)
                return "No tables found.";

            var sb = new StringBuilder();
            int tableIndex = 1;

            foreach (var table in tables)
            {
                sb.AppendLine($"### Table {tableIndex++}");
                var rows = table.SelectNodes(".//tr");
                if (rows == null) continue;

                foreach (var row in rows)
                {
                    var cells = row.SelectNodes("./th|./td");
                    if (cells == null) continue;

                    sb.AppendLine("| " + string.Join(" | ", cells.Select(c => c.InnerText.Trim())) + " |");
                }
                sb.AppendLine();
            }

            return sb.ToString();
        }
    }
}

