using DuckDuckGoDotNet;

using Microsoft.SemanticKernel;

using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace DeskMind.Plugins.WebSearch.Duck
{
    public class WebSearchPlugin
    {
        public WebSearchPlugin()
        {
        }

        [KernelFunction, Description("Scrape data from a web page based on XML extraction rules. The Result is a list of results in json format. Each Result contains, name of the site, short description, link to the site")]
        public async Task<string> SearchWeb(
            [Description("Search Term"), Required] string url, [Description("Expected number of results. Default value is 5"), Required] int maxResults = 5)
        {
            try
            {
                var search = new DuckDuckGoSearch();
                var results = await search.TextAsync(url, maxResults: maxResults);
                var json = System.Text.Json.JsonSerializer.Serialize(results);
                return json;
            }
            catch (System.Exception ex)
            {
                return $"Error during web search: {ex.Message}";
            }
        }
    }
}