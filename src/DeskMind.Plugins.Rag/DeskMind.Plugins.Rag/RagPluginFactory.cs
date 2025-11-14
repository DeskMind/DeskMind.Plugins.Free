using DeskMind.Core.Plugins;
using DeskMind.Plugins.Rag;

using Microsoft.SemanticKernel;

using System.Collections.Generic;

namespace DeskMind.Plugin.PythonRunner
{
    /// <summary>
    /// Plugin factory for the Python RAG SK tool.
    /// This exposes the Python knowledge base as a Semantic Kernel plugin
    /// which can be enabled/disabled via DeskMind's security policy.
    /// </summary>
    public class RagPluginFactory : SecurePluginFactoryBase
    {
        public override PluginMetadata Metadata => new PluginMetadata
        {
            Name = "Python RAG Plugin",
            Version = "1.0.0",
            Description = "This plugin provides access to the knowledge resources built-in to the application. The model could invoke following functions:\r\n  - rag.list_sources() → lists all RAG sources.\r\n- rag.search_source(sourceName, query, topK) → search a specific source.\r\n- rag.search_all_sources(query, topK) → search across all sources.",
            Dependencies = new[] { "DeskMind.Rag", "Local SQLite Vector Store" },
        };

        /// <summary>
        /// Logical RAG source name in IRagHub (e.g. 'python_runner_kb').
        /// </summary>
        public string SourceName
        {
            get
            {
                var entry = Configurations.Find(c => c.Name == nameof(SourceName));
                return entry != null && !string.IsNullOrEmpty(entry.SerializedValue)
                    ? entry.SerializedValue
                    : "python_runner_kb";
            }
            set
            {
                var serialized = string.IsNullOrWhiteSpace(value) ? "python_runner_kb" : value;
                var idx = Configurations.FindIndex(c => c.Name == nameof(SourceName));
                if (idx >= 0)
                    Configurations[idx] = new PluginConfig(nameof(SourceName), typeof(string).FullName, serialized);
                else
                    Configurations.Add(new PluginConfig(nameof(SourceName), typeof(string).FullName, serialized));
            }
        }

        /// <summary>
        /// Default TopK for RAG retrieval when the tool is called without explicit topK.
        /// </summary>
        public int DefaultTopK
        {
            get
            {
                var entry = Configurations.Find(c => c.Name == nameof(DefaultTopK));
                return entry != null && int.TryParse(entry.SerializedValue, out var t) ? t : 5;
            }
            set
            {
                var v = value <= 0 ? 5 : value;
                var idx = Configurations.FindIndex(c => c.Name == nameof(DefaultTopK));
                if (idx >= 0)
                    Configurations[idx] = new PluginConfig(nameof(DefaultTopK), typeof(int).FullName, v.ToString());
                else
                    Configurations.Add(new PluginConfig(nameof(DefaultTopK), typeof(int).FullName, v.ToString()));
            }
        }

        public override bool IsAvailable()
        {
            // RAG availability can be tightened if needed:
            // - check that DB file exists
            // - check RagHost.Hub != null
            // For now, rely on security policy to enable/disable.
            return true;
        }

        protected override KernelPlugin CreatePluginCore()
        {
            // Read configuration
            var topK = DefaultTopK;

            var pluginObject = new RagKernelPlugin();
            return KernelPluginFactory.CreateFromObject(pluginObject);
        }

        protected override List<PluginConfig> GetDefaultConfigurations()
        {
            return new List<PluginConfig>
            {
                new PluginConfig(nameof(DefaultTopK), typeof(int).FullName, "5")
            };
        }
    }
}