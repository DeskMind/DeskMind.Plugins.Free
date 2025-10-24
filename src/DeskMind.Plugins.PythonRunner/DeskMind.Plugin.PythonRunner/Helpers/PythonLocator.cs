using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

#if WINDOWS
using Microsoft.Win32;
#endif

namespace DeskMind.Plugin.PythonRunner.Helpers;

public static class PythonLocator
{
    /// <summary>
    /// Tries multiple strategies to find the default python executable and returns its directory (or null if not found).
    /// Strategies:
    /// 1. PYTHONHOME / PYENV / custom env vars
    /// 2. py launcher (Windows) -> python executable via "py -3 -c ..."
    /// 3. python / python3 command -> run small -c snippet to print sys.executable
    /// 4. where (Windows) or which (Unix) to locate executables on PATH
    /// 5. Windows registry (HKLM/HKCU PythonCore entries)
    /// </summary>
    public static async Task<string?> GetDefaultPythonExecutableDirectoryAsync()
    {
        // 1) Environment variables often reveal an install location
        var envCandidates = new[] { "PYTHONHOME", "PYENV_ROOT", "PYENV" };
        foreach (var env in envCandidates)
        {
            var val = Environment.GetEnvironmentVariable(env);
            if (!string.IsNullOrWhiteSpace(val))
            {
                // if env points at install root, try python inside it
                var candidateExe = Path.Combine(val.Trim('"'), "python.exe");
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && File.Exists(candidateExe))
                    return Path.GetDirectoryName(candidateExe);

                // unix/mac common
                candidateExe = Path.Combine(val.Trim('"'), "bin", "python");
                if (File.Exists(candidateExe))
                    return Path.GetDirectoryName(candidateExe);
            }
        }

        // 2) Try py launcher (Windows) first — often points to system Python used by default
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var exe = await RunPythonSnipWithLauncherAsync("py", "-3");
            if (!string.IsNullOrEmpty(exe) && File.Exists(exe))
                return Path.GetDirectoryName(exe);
        }

        // 3) Try common executables by running them and asking for sys.executable
        var pythonCandidates = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? new[] { "python", "python3" } // windows 'python' might be present via MS store
            : new[] { "python3", "python" };

        foreach (var candidate in pythonCandidates)
        {
            var exe = await RunPythonSnipWithLauncherAsync(candidate, "");
            if (!string.IsNullOrEmpty(exe) && File.Exists(exe))
                return Path.GetDirectoryName(exe);
        }

        // 4) Try where/which to find executable paths and pick the first valid one
        var foundByPath = await FindExecutableOnPathAsync(new[] { "py", "python3", "python" });
        if (!string.IsNullOrEmpty(foundByPath))
            return Path.GetDirectoryName(foundByPath);

        // 5) On Windows, try registry (name(s) under HKLM/HKCU\SOFTWARE\Python\PythonCore\<version>\InstallPath)
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var registryPath = QueryPythonFromWindowsRegistry();
            if (!string.IsNullOrEmpty(registryPath))
                return Path.GetDirectoryName(registryPath);
        }

        // nothing found
        return null;
    }

    private static async Task<string?> RunPythonSnipWithLauncherAsync(string launcher, string launcherVersionArg)
    {
        // Build the -c snippet to print sys.executable
        // Use quoting appropriate for platform (ProcessStartInfo.Arguments is literal, so keep double quotes for the python code).
        // Snippet: import sys, json; print(sys.executable)
        string code = "import sys, json; print(sys.executable)";
        string args;

        // For 'py' on Windows we usually want '-3 -c "code"'
        if (!string.IsNullOrEmpty(launcherVersionArg))
            args = $"{launcherVersionArg} -c \"{code}\"";
        else
            args = $"-c \"{code}\"";

        // When launcher is 'python' or 'python3', just pass -c "..."
        if (!launcher.Equals("py", StringComparison.OrdinalIgnoreCase) && string.IsNullOrEmpty(launcherVersionArg))
            args = $"-c \"{code}\"";

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = launcher,
                Arguments = args,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var proc = Process.Start(psi);
            if (proc == null) return null;

            // limit read time
            var readOutput = proc.StandardOutput.ReadToEndAsync();
            var readError = proc.StandardError.ReadToEndAsync();

            // wait for the process to exit (with timeout)
            var finished = await Task.Run(() => proc.WaitForExit(4000)); // 4s timeout
            var output = await readOutput;
            var error = await readError;

            if (!finished)
            {
                try { proc.Kill(); } catch { }
                return null;
            }

            output = output?.Trim();
            if (!string.IsNullOrEmpty(output) && File.Exists(output))
                return output;

            // Some setups print paths with quotes, or extra text — try to extract something that looks like a path
            if (!string.IsNullOrEmpty(output))
            {
                var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                                  .Select(l => l.Trim().Trim('"')).ToArray();
                foreach (var line in lines)
                {
                    if (File.Exists(line)) return line;
                }
            }
        }
        catch
        {
            // ignore launcher not found or execution errors
        }

        return null;
    }

    private static async Task<string?> FindExecutableOnPathAsync(string[] names)
    {
        string finder = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "where" : "which";
        foreach (var n in names)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = finder,
                    Arguments = n,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var p = Process.Start(psi);
                if (p == null) continue;

                var outText = await p.StandardOutput.ReadToEndAsync();
                p.WaitForExit(2000);
                outText = outText?.Trim();
                if (!string.IsNullOrEmpty(outText))
                {
                    // 'where' can return multiple lines; choose the first that exists
                    var first = outText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                                       .Select(x => x.Trim().Trim('"'))
                                       .FirstOrDefault(x => !string.IsNullOrEmpty(x) && File.Exists(x));
                    if (!string.IsNullOrEmpty(first))
                        return first;
                }
            }
            catch
            {
                // ignore
            }
        }

        return null;
    }

#if WINDOWS
    private static string? QueryPythonFromWindowsRegistry()
    {
        // Check both HKLM and HKCU, and both 64/32-bit paths where applicable.
        var hkRoots = new[] { Registry.LocalMachine, Registry.CurrentUser };
        foreach (var root in hkRoots)
        {
            try
            {
                using var baseKey = root.OpenSubKey(@"SOFTWARE\Python\PythonCore", writable: false);
                if (baseKey == null) continue;

                foreach (var version in baseKey.GetSubKeyNames().OrderByDescending(v => v))
                {
                    using var versionKey = baseKey.OpenSubKey(version);
                    if (versionKey == null) continue;

                    // InstallPath is usually present
                    using var installPathKey = versionKey.OpenSubKey("InstallPath");
                    if (installPathKey != null)
                    {
                        var installPath = installPathKey.GetValue("") as string ?? installPathKey.GetValue("ExecutablePath") as string;
                        if (!string.IsNullOrEmpty(installPath))
                        {
                            // Possibly the value is a folder, or a full path to exe
                            if (File.Exists(installPath)) return installPath;
                            var candidate = Path.Combine(installPath, "python.exe");
                            if (File.Exists(candidate)) return candidate;
                        }
                    }

                    // Also check for "SysExecutable" values or similar
                    var exe = versionKey.GetValue("SysExecutable") as string;
                    if (!string.IsNullOrEmpty(exe) && File.Exists(exe))
                        return exe;
                }
            }
            catch
            {
                // ignore permission or other registry errors
            }
        }

        // Fall back: check Wow6432Node path
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Wow6432Node\Python\PythonCore");
            if (key != null)
            {
                foreach (var version in key.GetSubKeyNames().OrderByDescending(v => v))
                {
                    using var versionKey = key.OpenSubKey(version);
                    using var ip = versionKey?.OpenSubKey("InstallPath");
                    var installPath = ip?.GetValue("") as string;
                    if (!string.IsNullOrEmpty(installPath))
                    {
                        var candidate = Path.Combine(installPath, "python.exe");
                        if (File.Exists(candidate)) return candidate;
                    }
                }
            }
        }
        catch { }

        return null;
    }
#else
    private static string? QueryPythonFromWindowsRegistry() => null;
#endif
}

