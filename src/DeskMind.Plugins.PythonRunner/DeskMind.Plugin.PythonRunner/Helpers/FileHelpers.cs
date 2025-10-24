using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace DeskMind.Plugin.PythonRunner.Helpers
{
    public class FileHelpers
    {
        public static async Task<string> ReadFileAsync(string path)
        {
            using var reader = new StreamReader(path);
            return await reader.ReadToEndAsync();
        }
    }
}

