using DeskMind.Rag.Abstractions;
using DeskMind.Rag.Hosting;

using Microsoft.SemanticKernel;

using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeskMind.Plugins.Rag
{
    /// <summary>
    /// SK-native plugin that exposes Python RAG search as a tool function.
    /// The IRagHub instance is provided via RagHost.Hub set by the host.
    /// </summary>
    public sealed class RagKernelPlugin
    {
        [KernelFunction("list_sources")]
        [Description("List all available RAG sources that can be used for retrieval.")]
        public string ListSources()
        {
            // Simple text output; you can expand later with descriptions if you store metadata.
            var sb = new StringBuilder();
            sb.AppendLine("Available RAG sources:");

            foreach (var name in RagHost.Hub.Sources)
            {
                sb.AppendLine($"- {name}");
            }

            return sb.ToString();
        }

        [KernelFunction("search_source")]
        [Description("Search a specific RAG source and return the most relevant snippets.")]
        public async Task<string> SearchSourceAsync(
     [Description("Name of the RAG source, e.g. 'python_runner_kb'.")]
            string sourceName,
     [Description("Natural language query to search in that source.")]
            string query,
     [Description("Maximum number of snippets to return.")]
            int topK = 5)
        {
            var hits = await RagHost.Hub.SearchAsync(sourceName, query, topK);

            var sb = new StringBuilder();
            sb.AppendLine($"Source: {sourceName}");
            sb.AppendLine();

            foreach (var hit in hits)
            {
                // Build a simple citation ID the model can echo back
                var id = $"DOC:{sourceName}#{hit.ChunkId}";
                sb.AppendLine($"[{id}] (score={hit.Score:F3})");
                sb.AppendLine(hit.Text);
                sb.AppendLine();
            }

            return sb.ToString();
        }

        [KernelFunction("search_all_sources")]
        [Description("Search across all RAG sources and return the most relevant snippets.")]
        public async Task<string> SearchAllSourcesAsync(
    [Description("Natural language query to search in all sources.")]
            string query,
    [Description("Maximum number of snippets to return in total.")]
            int topK = 5)
        {
            var sb = new StringBuilder();
            var allHits = new List<(string Source, SearchHit Hit)>();

            foreach (var sourceName in RagHost.Hub.Sources)
            {
                var hits = await RagHost.Hub.SearchAsync(sourceName, query, topK);
                allHits.AddRange(hits.Select(h => (sourceName, h)));
            }

            // Take the best overall topK by score
            var topOverall = allHits
                .OrderByDescending(h => h.Hit.Score)
                .Take(topK)
                .ToList();

            sb.AppendLine("Merged results from all sources:");
            sb.AppendLine();

            foreach (var entry in topOverall)
            {
                var source = entry.Source;
                var hit = entry.Hit;

                var id = $"DOC:{source}#{hit.ChunkId}";
                sb.AppendLine($"[{id}] (source={source}, score={hit.Score:F3})");
                sb.AppendLine(hit.Text);
                sb.AppendLine();
            }

            return sb.ToString();
        }
    }
}