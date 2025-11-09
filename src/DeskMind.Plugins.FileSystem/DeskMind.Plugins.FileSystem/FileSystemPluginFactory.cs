using DeskMind.Core.Plugins;

using Microsoft.SemanticKernel;

using System;
using System.Collections.Generic;
using System.IO;

namespace DeskMind.Plugins.FileSystem
{
    public class FileSystemPluginFactory : SecurePluginFactoryBase
    {
        public override PluginMetadata Metadata => new PluginMetadata
        {
            Name = "File System Plugin",
            Version = "1.0.0",
            Description = "Provide functions to the LLM to manipulate files and their content in a given workspace directory.",
        };

        public override bool IsAvailable()
        {
            // Requires .NET + WebScraper dependencies
            return true;
        }

        protected override KernelPlugin CreatePluginCore()
        {
            // Resolve workspace from configuration and ensure it exists
            var defaultWorkspace = Path.Combine(AppContext.BaseDirectory, "Workspace");
            var workspacePath = GetConfigValue<string>("WorkspacePath", defaultWorkspace) ?? defaultWorkspace;

            if (!Directory.Exists(workspacePath))
            {
                Directory.CreateDirectory(workspacePath);
            }

            var plugin = new FileSystemPlugin(workspacePath);
            return KernelPluginFactory.CreateFromObject(plugin);
        }

        /// <summary>
        /// Provide defaults if no config file is present.
        /// </summary>
        protected override List<PluginConfig> GetDefaultConfigurations()
        {
            return new List<PluginConfig>
            {
                new PluginConfig ("WorkspacePath", typeof(string).FullName ?? "string", Path.Combine(AppContext.BaseDirectory, "Workspace")),
            };
        }
    }
}