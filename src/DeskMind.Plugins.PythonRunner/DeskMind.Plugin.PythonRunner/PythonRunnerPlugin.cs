using DeskMind.Plugin.PythonRunner.Helpers;
using DeskMind.Plugin.PythonRunner.Services;

using Microsoft.SemanticKernel;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace DeskMind.Plugin.PythonRunner
{
    public class PythonRunnerPlugin
    {
        #region Fields

        private readonly string _scriptFolder;

        private readonly PythonVmCacheManager _vmCache;
        private readonly PythonProcessManager _proc = new PythonProcessManager();

        #endregion Fields

        public PythonRunnerPlugin(string scriptFolder, int defaultTimeout = 30)
        {
            _scriptFolder = scriptFolder;
            _vmCache = new PythonVmCacheManager(defaultTimeout: defaultTimeout);

            if (!Directory.Exists(_scriptFolder))
                Directory.CreateDirectory(_scriptFolder);
        }

        #region Kernel Functions

        /// <summary>
        /// Function returning all the available scripts
        /// </summary>
        /// <returns></returns>
        [KernelFunction, Description("List all Python scripts in the configured folder.")]
        public IEnumerable<string> ListScripts()
                    => Directory.Exists(_scriptFolder)
                        ? Directory.GetFiles(_scriptFolder, "*.py").Select(f => Path.GetFileNameWithoutExtension(f))
                        : new List<string>();

        /// <summary>
        /// Run a pre-defined python script stored in the script dir
        /// </summary>
        /// <param name="scriptName"></param>
        /// <param name="argsJson"></param>
        /// <param name="timeoutSeconds"></param>
        /// <returns></returns>
        [KernelFunction, Description("Run a Python script with JSON arguments using cached Python VMs.")]
        public async Task<string> RunScript(
           [Description("Script name without extension"), Required] string scriptName,
           [Description("Arguments as JSON string")] string argsJson = "{}",
           [Description("Timeout in seconds")] int timeoutSeconds = 30)
        {
            var scriptPath = Path.Combine(_scriptFolder, scriptName + ".py");
            if (!File.Exists(scriptPath))
                return JsonSerializer.Serialize(new { error = $"Script '{scriptName}' not found." });

            // 🔹 Validate the script before running
            var code = await FileHelpers.ReadFileAsync(scriptPath);
            if (!PythonScriptValidator.Validate(code, out var validationError))
            {
                return JsonSerializer.Serialize(new { error = $"Script validation failed: {validationError}" });
            }

            // Check dependencies
            var deps = await EnsureDependenciesAsync(scriptPath, code, timeoutSeconds);
            if (!deps.Ok)
                return JsonSerializer.Serialize(new { error = deps.Error });

            Dictionary<string, object> argsObj;
            try
            {
                argsObj = string.IsNullOrWhiteSpace(argsJson)
                    ? new Dictionary<string, object>()
                    : JsonSerializer.Deserialize<Dictionary<string, object>>(argsJson);
            }
            catch (JsonException)
            {
                return JsonSerializer.Serialize(new { error = "Invalid JSON arguments." });
            }

            var (success, output, error) = await _vmCache.RunCachedScriptAsync(scriptPath, argsObj, timeoutSeconds);

            if (!success)
                return JsonSerializer.Serialize(new { error });

            try
            {
                var parsed = JsonSerializer.Deserialize<object>(output);
                return JsonSerializer.Serialize(parsed);
            }
            catch
            {
                return JsonSerializer.Serialize(new { result = output });
            }
        }

        /// <summary>
        /// Function Running inline code directly from the tool call
        /// </summary>
        /// <param name="code"></param>
        /// <param name="argsJson"></param>
        /// <param name="timeoutSeconds"></param>
        /// <param name="codeKey"></param>
        /// <returns></returns>
        [KernelFunction, Description("Run ad-hoc Python code (provided as a string) with JSON arguments using cached Python VMs.")]
        public async Task<string> RunInlineCode(
    [Description("Python code to execute. Only excepting UTF8 encoded string."), Required] string code,
    [Description("Arguments as JSON string")] string argsJson = "{}",
    [Description("Timeout in seconds")] int timeoutSeconds = 30,
    [Description("Optional custom key to identify/cache this code. If not provided, a SHA256 of the code is used.")]
    string? codeKey = null)
        {
            if (string.IsNullOrWhiteSpace(code))
                return JsonSerializer.Serialize(new { error = "No code provided." });

            // Normalize: strip a leading UTF-8 BOM if present to avoid Python/parsers seeing U+FEFF
            if (code.Length > 0 && code[0] == '\uFEFF')
                code = code.Substring(1);

            // 🔹 Validate the code before running
            if (!PythonScriptValidator.Validate(code, out var validationError))
                return JsonSerializer.Serialize(new { error = $"Script validation failed: {validationError}" });

            Dictionary<string, object> argsObj;
            try
            {
                argsObj = string.IsNullOrWhiteSpace(argsJson)
                    ? new Dictionary<string, object>()
                    : JsonSerializer.Deserialize<Dictionary<string, object>>(argsJson);
            }
            catch (JsonException)
            {
                return JsonSerializer.Serialize(new { error = "Invalid JSON arguments." });
            }

            // 🔹 Ensure deterministic filename
            var key = string.IsNullOrWhiteSpace(codeKey) ? ComputeSha256(code) : SanitizeKey(codeKey);
            var scriptPath = Path.Combine(_scriptFolder, $"__inline_{key}.py");

            try
            {
                // Write script safely
                if (!Directory.Exists(_scriptFolder))
                    Directory.CreateDirectory(_scriptFolder);

                if (!File.Exists(scriptPath))
                {
                    await WriteTextAsync(scriptPath, code);
                }
                else
                {
                    var existing = await ReadTextAsync(scriptPath);
                    if (!string.Equals(existing, code, StringComparison.Ordinal))
                        await WriteTextAsync(scriptPath, code);
                }
            }
            catch (IOException ioex)
            {
                return JsonSerializer.Serialize(new { error = $"Failed to persist inline script: {ioex.Message}" });
            }

            // Check Dependencies
            var deps = await EnsureDependenciesAsync(scriptPath, code, timeoutSeconds);
            if (!deps.Ok)
                return JsonSerializer.Serialize(new { error = deps.Error });

            // 🔹 Execute
            var (success, output, error) = await _vmCache.RunCachedScriptAsync(scriptPath, argsObj, timeoutSeconds);

            if (!success)
                return JsonSerializer.Serialize(new { error });

            try
            {
                var parsed = JsonSerializer.Deserialize<object>(output);
                return JsonSerializer.Serialize(parsed);
            }
            catch
            {
                return JsonSerializer.Serialize(new { result = output });
            }
        }

        #endregion Kernel Functions

        #region Helpers

        // inside PythonRunnerPlugin
        private async Task<(bool Ok, string Error)> EnsureDependenciesAsync(
            string scriptPath,
            string code,
            int timeoutSeconds)
        {
            // Normalize: strip leading BOM to avoid U+FEFF confusing Python parsers
            if (!string.IsNullOrEmpty(code) && code[0] == '\uFEFF')
                code = code.Substring(1);

            // Simple idempotency: per-code marker next to script
            var codeHash = ComputeSha256(code);
            var depsMarker = Path.Combine(Path.GetDirectoryName(scriptPath) ?? _scriptFolder,
                                          $".deps_{codeHash}.ok");

            if (File.Exists(depsMarker)) return (true, "");

            // 1) Prefer a colocated requirements.txt if present
            var scriptDir = Path.GetDirectoryName(scriptPath) ?? _scriptFolder;
            var localReq = Path.Combine(scriptDir, "requirements.txt");
            string reqToUse = null;

            if (File.Exists(localReq))
            {
                reqToUse = localReq;
            }
            else
            {
                // 2) Generate ad-hoc requirements via pipreqs in an isolated temp dir
                var tmpDir = Path.Combine(Path.GetTempPath(), "ai-bridge-pyreqs-" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(tmpDir);

                var tmpScript = Path.Combine(tmpDir, Path.GetFileName(scriptPath));
                await WriteTextAsync(tmpScript, code);

                var genReq = Path.Combine(tmpDir, "requirements.txt");
                // Use python -m pipreqs <dir> --force --savepath <file> --encoding utf-8
                var (genOk, genOut, genErr) = await _proc.RunPythonModuleAsync(
                    "pipreqs.pipreqs",
                    $"\"{tmpDir}\" --force --savepath \"{genReq}\" --encoding=utf-8",
                    workingDirectory: tmpDir,
                    timeoutSeconds: timeoutSeconds
                );

                // pipreqs returns 0 even if empty sometimes; treat missing/empty as "no deps"
                if (!genOk)
                    return (false, $"pipreqs failed: {genErr}");

                if (File.Exists(genReq) && new FileInfo(genReq).Length > 0)
                {
                    reqToUse = genReq;
                }
            }

            if (!string.IsNullOrEmpty(reqToUse))
            {
                // 3) Install using pip; prefer user site to avoid admin rights
                // You could switch to a venv later; keeping minimal change now:
                var (ok, outp, err) = await _proc.RunPythonModuleAsync(
                    "pip",
                    $"install --disable-pip-version-check --user -r \"{reqToUse}\"",
                    workingDirectory: Path.GetDirectoryName(reqToUse),
                    timeoutSeconds: Math.Max(timeoutSeconds, 60) // installs can be slower
                );

                if (!ok) return (false, $"pip install failed: {err}");
            }

            // Mark success
            try { File.WriteAllText(depsMarker, DateTime.UtcNow.ToString("o")); } catch { /* ignore */ }
            return (true, "");
        }

        private static async Task<string> ReadTextAsync(string path)
        {
            using (var reader = new StreamReader(path, Encoding.UTF8))
                return await reader.ReadToEndAsync().ConfigureAwait(false);
        }

        private static async Task WriteTextAsync(string path, string content)
        {
            // Ensure we write UTF-8 without BOM so tools like pipreqs/ast don't see a leading U+FEFF
            var utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
            using (var writer = new StreamWriter(path, false, utf8NoBom))
                await writer.WriteAsync(content).ConfigureAwait(false);
        }

        private static string ComputeSha256(string input)
        {
            using (var sha = System.Security.Cryptography.SHA256.Create())
            {
                var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
                var sb = new StringBuilder(bytes.Length * 2);
                foreach (var b in bytes)
                    sb.Append(b.ToString("x2"));
                return sb.ToString();
            }
        }

        private static string SanitizeKey(string key)
        {
            var arr = key.Select(ch => char.IsLetterOrDigit(ch) ? ch : '_').ToArray();
            var sanitized = new string(arr).Trim('_');
            return string.IsNullOrWhiteSpace(sanitized) ? ComputeSha256(key) : sanitized;
        }

        #endregion Helpers
    }
}


