using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace DeskMind.Plugin.PythonRunner.Services
{
    public static class PythonScriptValidator
    {
        /// <summary>
        /// Validates a Python script to ensure it follows safe rules:
        /// - Must contain a `def run(input):` entry point
        /// - No forbidden keywords like exec/eval
        /// - No dangerous imports (os, sys, subprocess, etc.)
        /// </summary>
        public static bool Validate(string code, out string errorMessage)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                errorMessage = "Script is empty.";
                return false;
            }

            // Rule 1: Require run() function
            if (!Regex.IsMatch(code, @"def\s+run\s*\(", RegexOptions.Multiline))
            {
                errorMessage = "Missing required entry function: def run(input).";
                return false;
            }

            // Rule 2: Disallow dangerous imports
            var forbiddenImports = new[] { "os", "sys", "subprocess", "shutil", "socket" };
            foreach (var imp in forbiddenImports)
            {
                if (Regex.IsMatch(code, $@"\bimport\s+{imp}\b"))
                {
                    errorMessage = $"Forbidden import detected: {imp}";
                    return false;
                }
            }

            // Rule 3: Disallow exec/eval usage
            var forbiddenKeywords = new[] { "exec(", "eval(" };
            foreach (var keyword in forbiddenKeywords)
            {
                if (code.Contains(keyword))
                {
                    errorMessage = $"Forbidden usage detected: {keyword}";
                    return false;
                }
            }

            // Rule 4: Disallow __import__ to bypass validation
            if (code.Contains("__import__"))
            {
                errorMessage = "Forbidden usage detected: __import__";
                return false;
            }

            // ✅ Passed validation
            errorMessage = string.Empty;
            return true;
        }
    }
}

