using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json;
using System.Threading.Tasks;

namespace DeskMind.Plugin.PythonRunner.Services
{
    public class PythonProcessManager
    {
        private readonly string _pythonExe;
        private readonly int _defaultTimeout;

        public PythonProcessManager(string pythonExe = "python", int defaultTimeoutSeconds = 300)
        {
            _pythonExe = pythonExe;
            _defaultTimeout = defaultTimeoutSeconds;
        }

        public async Task<(bool Success, string Output, string Error)> RunScriptAsync(string scriptPath, Dictionary<string, object> args = null, int? timeoutSeconds = null)
        {
            if (!System.IO.File.Exists(scriptPath))
                return (false, "", $"Script not found: {scriptPath}");

            string argsJson = args != null && args.Count > 0  ? JsonSerializer.Serialize(args) : "";

            using var proc = new Process();
            proc.StartInfo.FileName = _pythonExe;
            proc.StartInfo.Arguments = $"{scriptPath} {argsJson}";
            proc.StartInfo.RedirectStandardOutput = true;
            proc.StartInfo.RedirectStandardError = true;
            proc.StartInfo.UseShellExecute = false;
            proc.StartInfo.CreateNoWindow = true;

            proc.Start();

            var outputTask = proc.StandardOutput.ReadToEndAsync();
            var errorTask = proc.StandardError.ReadToEndAsync();

            bool exited = proc.WaitForExit((timeoutSeconds ?? _defaultTimeout) * 1000);
            if (!exited)
            {
                try { proc.Kill(); } catch { }
                return (false, "", "Script timed out.");
            }

            var output = await outputTask;
            var error = await errorTask;

            bool success = string.IsNullOrWhiteSpace(error) && proc.ExitCode == 0;
            return (success, output.Trim(), error.Trim());
        }

        public async Task<(bool Success, string Output, string Error)> RunPythonModuleAsync(
            string module,
            string arguments,
            string workingDirectory = null,
            int? timeoutSeconds = null)
        {
            using var proc = new Process();
            proc.StartInfo.FileName = _pythonExe;
            proc.StartInfo.Arguments = $"-m {module} {arguments}";
            proc.StartInfo.RedirectStandardOutput = true;
            proc.StartInfo.RedirectStandardError = true;
            proc.StartInfo.UseShellExecute = false;
            proc.StartInfo.CreateNoWindow = true;
            if (!string.IsNullOrWhiteSpace(workingDirectory))
                proc.StartInfo.WorkingDirectory = workingDirectory;

            proc.Start();

            var outputTask = proc.StandardOutput.ReadToEndAsync();
            var errorTask = proc.StandardError.ReadToEndAsync();

            bool exited = proc.WaitForExit((timeoutSeconds ?? _defaultTimeout) * 1000);
            if (!exited)
            {
                try { proc.Kill(); } catch { }
                return (false, "", $"Module '{module}' timed out.");
            }

            var output = await outputTask;
            var error = await errorTask;
            bool success = proc.ExitCode == 0;
            return (success, output.Trim(), error.Trim());
        }
    }
}

