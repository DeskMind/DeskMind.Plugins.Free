using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace DeskMind.Plugin.PythonRunner.Services
{
    public class PythonVmCacheManager
    {
        private readonly string _cachePersistPath;
        private readonly ConcurrentDictionary<string, Process> _processCache = new();
        private readonly string _pythonExe;
        private readonly int _defaultTimeout;

        public PythonVmCacheManager(string pythonExe = "python", int defaultTimeout = 30, string persistPath = null)
        {
            _pythonExe = pythonExe;
            _defaultTimeout = defaultTimeout;
            _cachePersistPath = persistPath ?? Path.Combine(AppContext.BaseDirectory, "python_vm_cache.json");

            LoadCache();
        }

        private void LoadCache()
        {
            if (!File.Exists(_cachePersistPath)) return;
            try
            {
                var cachedScripts = JsonSerializer.Deserialize<string[]>(File.ReadAllText(_cachePersistPath));
                if (cachedScripts != null)
                {
                    foreach (var script in cachedScripts)
                    {
                        if (!_processCache.ContainsKey(script))
                            _processCache[script] = null; // Mark as to-be-initialized
                    }
                }
            }
            catch { }
        }

        private void PersistCache()
        {
            var keys = _processCache.Keys;
            File.WriteAllText(_cachePersistPath, JsonSerializer.Serialize(keys));
        }

        private Process StartPythonProcess(string scriptPath)
        {
            if (!File.Exists(scriptPath))
                throw new FileNotFoundException($"Script not found: {scriptPath}");

            var proc = new Process();
            proc.StartInfo.FileName = _pythonExe;
            proc.StartInfo.Arguments = $"\"{scriptPath}\" \"{{}}\""; // Empty JSON args for initialization
            proc.StartInfo.RedirectStandardOutput = true;
            proc.StartInfo.RedirectStandardError = true;
            proc.StartInfo.UseShellExecute = false;
            proc.StartInfo.CreateNoWindow = true;
            proc.EnableRaisingEvents = true;

            proc.Start();
            return proc;
        }

        public async Task<(bool Success, string Output, string Error)> RunCachedScriptAsync(string scriptPath, Dictionary<string, object> args = null, int? timeoutSeconds = null)
        {
            if (!_processCache.ContainsKey(scriptPath))
            {
                var proc = StartPythonProcess(scriptPath);
                _processCache[scriptPath] = proc;
                PersistCache();
            }

            // For simplicity, run a new process per call but maintain cached dictionary
            var processManager = new PythonProcessManager(_pythonExe, _defaultTimeout);
            return await processManager.RunScriptAsync(scriptPath, args, timeoutSeconds ?? _defaultTimeout);
        }
    }
}

