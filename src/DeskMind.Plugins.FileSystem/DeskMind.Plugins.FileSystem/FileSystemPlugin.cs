using Microsoft.SemanticKernel;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Text;

namespace DeskMind.Plugins.FileSystem
{
    public class FileSystemPlugin
    {
        private readonly string _workspacePath;

        /// <summary>
        /// Constructor accepts configuration paths.
        /// </summary>
        /// <param name="workspacePath">Path to workspace path</param>
        public FileSystemPlugin(string workspacePath)
        {
            _workspacePath = Path.GetFullPath(workspacePath ?? throw new ArgumentNullException(nameof(workspacePath)));
        }

        // --- Helpers ---
        private string ResolveWorkspacePath(string relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
                throw new ValidationException("Path must be a non-empty relative path.");

            if (Path.IsPathRooted(relativePath))
                throw new ValidationException("Absolute paths are not allowed. Use paths relative to the workspace.");

            var combined = Path.Combine(_workspacePath, relativePath);
            var full = Path.GetFullPath(combined);

            // Ensure within workspace
            var root = _workspacePath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
            if (!full.StartsWith(root, StringComparison.OrdinalIgnoreCase) && !string.Equals(full, _workspacePath, StringComparison.OrdinalIgnoreCase))
            {
                throw new ValidationException("Resolved path escapes the workspace boundary.");
            }

            return full;
        }

        private string ToWorkspaceRelative(string fullPath)
        {
            var root = _workspacePath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
            fullPath = Path.GetFullPath(fullPath);
            if (fullPath.StartsWith(root, StringComparison.OrdinalIgnoreCase))
            {
                var rel = fullPath.Substring(root.Length);
                return rel.Replace(Path.DirectorySeparatorChar, '/');
            }
            if (string.Equals(fullPath, _workspacePath, StringComparison.OrdinalIgnoreCase))
            {
                return string.Empty;
            }
            return fullPath;
        }

        private static Encoding GetEncodingOrDefault(string encodingName)
        {
            if (string.IsNullOrWhiteSpace(encodingName)) return new UTF8Encoding(false);
            try { return Encoding.GetEncoding(encodingName); }
            catch { return new UTF8Encoding(false); }
        }

        [KernelFunction, Description("Check if the provided directory exists relative to the workspace path.")]
        public bool DirectoryExists(
               [Description("File path relative to workspace directory."), Required] string directoryPath)
        {
            var path = ResolveWorkspacePath(directoryPath);
            return Directory.Exists(path);
        }

        [KernelFunction, Description("Check if the file exists in the given path, relative to the workspace path.")]
        public bool FileExists(
            [Description("Directory path relative to workspace directory"), Required] string filePath)
        {
            var path = ResolveWorkspacePath(filePath);
            return File.Exists(path);
        }

        [KernelFunction, Description("Create a directory (and any missing parents) relative to the workspace. Returns the created directory path (relative).")]
        public string CreateDirectory(
            [Description("Directory path to create, relative to the workspace."), Required] string directoryPath)
        {
            var path = ResolveWorkspacePath(directoryPath);
            Directory.CreateDirectory(path);
            return ToWorkspaceRelative(path);
        }

        [KernelFunction, Description("Delete a directory relative to the workspace.")]
        public bool DeleteDirectory(
            [Description("Directory path to delete, relative to the workspace."), Required] string directoryPath,
            [Description("Delete recursively if true."), Required] bool recursive)
        {
            var path = ResolveWorkspacePath(directoryPath);
            if (!Directory.Exists(path)) return false;
            Directory.Delete(path, recursive);
            return true;
        }

        [KernelFunction, Description("List files in a directory relative to the workspace.")]
        public string[] ListFiles(
            [Description("Directory path to search, relative to the workspace."), Required] string directoryPath,
            [Description("Search pattern like *.cs or * (default: *)."), Required] string searchPattern,
            [Description("Search recursively if true."), Required] bool recursive)
        {
            var path = ResolveWorkspacePath(directoryPath);
            if (!Directory.Exists(path)) return new string[0];
            var option = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            var files = Directory.GetFiles(path, string.IsNullOrWhiteSpace(searchPattern) ? "*" : searchPattern, option)
                .Select(ToWorkspaceRelative)
                .ToArray();
            return files;
        }

        [KernelFunction, Description("List sub-directories in a directory relative to the workspace.")]
        public string[] ListDirectories(
            [Description("Directory path to search, relative to the workspace."), Required] string directoryPath,
            [Description("Search recursively if true."), Required] bool recursive)
        {
            var path = ResolveWorkspacePath(directoryPath);
            if (!Directory.Exists(path)) return new string[0];
            var option = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            var dirs = Directory.GetDirectories(path, "*", option)
                .Select(ToWorkspaceRelative)
                .ToArray();
            return dirs;
        }

        [KernelFunction, Description("Read all text from a file relative to the workspace.")]
        public string ReadText(
            [Description("File path to read, relative to the workspace."), Required] string filePath,
            [Description("Text encoding name (default UTF-8)."), Required] string encoding)
        {
            var path = ResolveWorkspacePath(filePath);
            var enc = GetEncodingOrDefault(encoding);
            return File.ReadAllText(path, enc);
        }

        [KernelFunction, Description("Write text to a file relative to the workspace. Creates directories as needed.")]
        public bool WriteText(
            [Description("File path to write, relative to the workspace."), Required] string filePath,
            [Description("Content to write."), Required] string content,
            [Description("Overwrite if file exists (default true)."), Required] bool overwrite,
            [Description("Text encoding name (default UTF-8)."), Required] string encoding)
        {
            var path = ResolveWorkspacePath(filePath);
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

            if (File.Exists(path) && !overwrite) return false;

            var enc = GetEncodingOrDefault(encoding);
            File.WriteAllText(path, content ?? string.Empty, enc);
            return true;
        }

        [KernelFunction, Description("Append text to a file relative to the workspace. Creates directories as needed.")]
        public bool AppendText(
            [Description("File path to append, relative to the workspace."), Required] string filePath,
            [Description("Content to append."), Required] string content,
            [Description("Text encoding name (default UTF-8)."), Required] string encoding)
        {
            var path = ResolveWorkspacePath(filePath);
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            var enc = GetEncodingOrDefault(encoding);
            File.AppendAllText(path, content ?? string.Empty, enc);
            return true;
        }

        [KernelFunction, Description("Delete a file relative to the workspace.")]
        public bool DeleteFile(
            [Description("File path to delete, relative to the workspace."), Required] string filePath)
        {
            var path = ResolveWorkspacePath(filePath);
            if (!File.Exists(path)) return false;
            File.Delete(path);
            return true;
        }

        [KernelFunction, Description("Copy a file within the workspace.")]
        public bool CopyFile(
            [Description("Source file path relative to workspace."), Required] string sourceFilePath,
            [Description("Destination file path relative to workspace."), Required] string destinationFilePath,
            [Description("Overwrite if exists (default true)."), Required] bool overwrite)
        {
            var src = ResolveWorkspacePath(sourceFilePath);
            var dst = ResolveWorkspacePath(destinationFilePath);
            var dstDir = Path.GetDirectoryName(dst);
            if (!string.IsNullOrEmpty(dstDir)) Directory.CreateDirectory(dstDir);

            File.Copy(src, dst, overwrite);
            return true;
        }

        [KernelFunction, Description("Move or rename a file within the workspace.")]
        public bool MoveFile(
            [Description("Source file path relative to workspace."), Required] string sourceFilePath,
            [Description("Destination file path relative to workspace."), Required] string destinationFilePath,
            [Description("Overwrite if exists (default true)."), Required] bool overwrite)
        {
            var src = ResolveWorkspacePath(sourceFilePath);
            var dst = ResolveWorkspacePath(destinationFilePath);
            var dstDir = Path.GetDirectoryName(dst);
            if (!string.IsNullOrEmpty(dstDir)) Directory.CreateDirectory(dstDir);

            if (File.Exists(dst))
            {
                if (!overwrite) return false;
                File.Delete(dst);
            }
            File.Move(src, dst);
            return true;
        }

        [KernelFunction, Description("Copy a directory (recursively) within the workspace.")]
        public bool CopyDirectory(
            [Description("Source directory path relative to workspace."), Required] string sourceDirectoryPath,
            [Description("Destination directory path relative to workspace."), Required] string destinationDirectoryPath,
            [Description("Overwrite files if they exist (default true)."), Required] bool overwrite)
        {
            var src = ResolveWorkspacePath(sourceDirectoryPath);
            var dst = ResolveWorkspacePath(destinationDirectoryPath);

            if (!Directory.Exists(src)) return false;
            Directory.CreateDirectory(dst);

            foreach (var dirPath in Directory.GetDirectories(src, "*", SearchOption.AllDirectories))
            {
                var rel = dirPath.Substring(src.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                Directory.CreateDirectory(Path.Combine(dst, rel));
            }

            foreach (var filePath in Directory.GetFiles(src, "*", SearchOption.AllDirectories))
            {
                var rel = filePath.Substring(src.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                var target = Path.Combine(dst, rel);
                var targetDir = Path.GetDirectoryName(target);
                if (!string.IsNullOrEmpty(targetDir)) Directory.CreateDirectory(targetDir);
                File.Copy(filePath, target, overwrite);
            }

            return true;
        }

        [KernelFunction, Description("Move a directory within the workspace.")]
        public bool MoveDirectory(
            [Description("Source directory path relative to workspace."), Required] string sourceDirectoryPath,
            [Description("Destination directory path relative to workspace."), Required] string destinationDirectoryPath,
            [Description("Overwrite destination if it exists (default false). When true and destination exists, it will be deleted before move."), Required] bool overwrite)
        {
            var src = ResolveWorkspacePath(sourceDirectoryPath);
            var dst = ResolveWorkspacePath(destinationDirectoryPath);
            if (!Directory.Exists(src)) return false;
            if (Directory.Exists(dst))
            {
                if (!overwrite) return false;
                Directory.Delete(dst, true);
            }
            var dstParent = Path.GetDirectoryName(dst);
            if (!string.IsNullOrEmpty(dstParent)) Directory.CreateDirectory(dstParent);
            Directory.Move(src, dst);
            return true;
        }

        [KernelFunction, Description("Get a text tree of the directory contents to a maximum depth. Useful for project scaffolding overview.")]
        public string GetDirectoryTree(
            [Description("Directory path relative to workspace (use '.' for root)."), Required] string directoryPath,
            [Description("Maximum depth (>=1)."), Required] int maxDepth)
        {
            if (maxDepth < 1) maxDepth = 1;
            var root = ResolveWorkspacePath(directoryPath);
            if (!Directory.Exists(root)) return string.Empty;

            var sb = new StringBuilder();
            void Recurse(string current, int depth, string indent)
            {
                if (depth > maxDepth) return;
                var name = current == root ? ToWorkspaceRelative(current) : Path.GetFileName(current);
                if (string.IsNullOrEmpty(name)) name = ".";
                if (current != root) sb.AppendLine(indent + name + "/");

                var files = Directory.GetFiles(current).OrderBy(Path.GetFileName).ToArray();
                foreach (var f in files)
                {
                    sb.AppendLine(indent + " " + Path.GetFileName(f));
                }

                var dirs = Directory.GetDirectories(current).OrderBy(Path.GetFileName).ToArray();
                foreach (var d in dirs)
                {
                    sb.AppendLine(indent + " " + Path.GetFileName(d) + "/");
                    Recurse(d, depth + 1, indent + " ");
                }
            }

            Recurse(root, 1, string.Empty);
            return sb.ToString();
        }
    }
}