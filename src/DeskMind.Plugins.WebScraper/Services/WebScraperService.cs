using DeskMind.Plugins.WebScraper.Models;

using HtmlAgilityPack;

using Microsoft.Playwright;

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DeskMind.Plugins.WebScraper.Services
{
    public class WebScraperService
    {
        public async Task<Dictionary<string, object>> ScrapeAsync(ScraperConfig config)
        {
            if (config.RequiresJavascript)
            {
                return await ScrapeDynamicAsync(config);
            }
            else
            {
                return ScrapeStatic(config);
            }
        }

        private Dictionary<string, object> ScrapeStatic(ScraperConfig config)
        {
            var doc = new HtmlWeb().Load(config.Url);
            var result = new Dictionary<string, object>();

            // Extract fields
            foreach (var field in config.Fields)
            {
                var node = doc.DocumentNode.SelectSingleNode(GetXPath(field.Selector));
                var value = ExtractValue(node, field.Attribute);
                result[field.Name] = value;
            }

            // Extract collections
            foreach (var collection in config.Collections)
            {
                var nodes = doc.DocumentNode.SelectNodes(GetXPath(collection.Selector)) ?? Enumerable.Empty<HtmlNode>();
                var list = new List<Dictionary<string, string>>();

                foreach (var node in nodes)
                {
                    var item = new Dictionary<string, string>();
                    foreach (var field in collection.Fields)
                    {
                        var subNode = node.SelectSingleNode(GetXPath(field.Selector));
                        item[field.Name] = ExtractValue(subNode, field.Attribute);
                    }
                    list.Add(item);
                }
                result[collection.Name] = list;
            }

            return result;
        }

        private async Task<Dictionary<string, object>> ScrapeDynamicAsync(ScraperConfig config)
        {
            using var playwright = await Playwright.CreateAsync();
            await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
            var page = await browser.NewPageAsync();
            await page.GotoAsync(config.Url);

            if (!string.IsNullOrEmpty(config.WaitForSelector))
                await page.WaitForSelectorAsync(config.WaitForSelector);

            var result = new Dictionary<string, object>();

            // Extract fields
            foreach (var field in config.Fields)
            {
                var element = await page.QuerySelectorAsync(field.Selector);
                var value = await ExtractValueAsync(element, field.Attribute);
                result[field.Name] = value;
            }

            // Extract collections
            foreach (var collection in config.Collections)
            {
                var elements = await page.QuerySelectorAllAsync(collection.Selector);
                var list = new List<Dictionary<string, string>>();

                foreach (var element in elements)
                {
                    var item = new Dictionary<string, string>();
                    foreach (var field in collection.Fields)
                    {
                        var subNode = await element.QuerySelectorAsync(field.Selector);
                        item[field.Name] = await ExtractValueAsync(subNode, field.Attribute);
                    }
                    list.Add(item);
                }

                result[collection.Name] = list;
            }

            return result;
        }

        private string ExtractValue(HtmlNode node, string attribute)
        {
            if (node == null) return "";
            return attribute switch
            {
                "innerText" => node.InnerText.Trim(),
                _ => node.GetAttributeValue(attribute, "")
            };
        }

        private async Task<string> ExtractValueAsync(IElementHandle element, string attribute)
        {
            if (element == null) return "";
            if (attribute == "innerText")
                return (await element.InnerTextAsync()).Trim();
            return await element.GetAttributeAsync(attribute) ?? "";
        }

        // Simple CSS selector → XPath (basic support)
        private string GetXPath(string selector)
        {
            if (selector.StartsWith("//")) return selector; // already XPath
            // naive CSS selector translation
            return $"//*[contains(@class,'{selector.Trim('.')}')]";
        }
    }
}

