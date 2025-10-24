using DeskMind.Core.Plugins;

using Microsoft.SemanticKernel;

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace DeskMind.Plugin.PythonRunner
{
    public class PythonRunnerPluginFactory : SecurePluginFactoryBase
    {
        public override PluginMetadata Metadata => new PluginMetadata
        {
            Name = "Python Script Plugin",
            Version = "1.0.0",
            Description = "Allows running user-provided Python scripts as Semantic Kernel functions using cached Python VMs.",
            Dependencies = new[] { "Python 3.x" }
        };

        // Configurable script folder path
        public string ScriptFolder
        {
            get
            {
                var entry = Configurations.Find(c => c.Name == nameof(ScriptFolder));
                return entry != null ? entry.SerializedValue : Path.Combine(AppContext.BaseDirectory, "python_scripts");
            }
            set
            {
                var idx = Configurations.FindIndex(c => c.Name == nameof(ScriptFolder));
                if (idx >= 0)
                    Configurations[idx] = new PluginConfig(nameof(ScriptFolder), typeof(string).FullName, value);
                else
                    Configurations.Add(new PluginConfig(nameof(ScriptFolder), typeof(string).FullName, value));
            }
        }

        public int DefaultTimeout
        {
            get
            {
                var entry = Configurations.Find(c => c.Name == nameof(DefaultTimeout));
                return entry != null && int.TryParse(entry.SerializedValue, out var t) ? t : 30;
            }
            set
            {
                var idx = Configurations.FindIndex(c => c.Name == nameof(DefaultTimeout));
                if (idx >= 0)
                    Configurations[idx] = new PluginConfig(nameof(DefaultTimeout), typeof(int).FullName, value.ToString());
                else
                    Configurations.Add(new PluginConfig(nameof(DefaultTimeout), typeof(int).FullName, value.ToString()));
            }
        }

        public override bool IsAvailable()
        {
            // Always available if Python is installed (optional: check Python path)
            return true;
        }

        protected override KernelPlugin CreatePluginCore()
        {
            var plugin = new PythonRunnerPlugin(ScriptFolder, DefaultTimeout);
            return KernelPluginFactory.CreateFromObject(plugin);
        }

        protected override List<PluginConfig> GetDefaultConfigurations()
        {
            return new List<PluginConfig>
            {
                new PluginConfig(nameof(ScriptFolder), typeof(string).FullName, Path.Combine(AppContext.BaseDirectory, "python_scripts")),
                new PluginConfig(nameof(DefaultTimeout), typeof(int).FullName, "30")
            };
        }
    }
}

