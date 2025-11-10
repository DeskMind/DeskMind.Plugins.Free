using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using DeskMind.Core.UI;
using DeskMind.Plugins.FileSystem.WPF.Models;

using ICSharpCode.AvalonEdit.Document;

using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

using Wpf.Ui.Controls;

namespace DeskMind.Plugins.FileSystem.WPF.ViewModels
{
    public partial class FileSystemManagerViewModel : ObservableObject
    {
        private readonly string _workspacePath;
        private readonly IMessageBoxService _messageBoxService;

        public ObservableCollection<FileNode> Items { get; } = new();

        [ObservableProperty]
        private FileNode? _selectedNode;

        [ObservableProperty]
        private TextDocument _document = new();

        [ObservableProperty]
        private string _currentFilePath = string.Empty;

        [ObservableProperty]
        private bool _isDirty;

        public IRelayCommand RefreshCommand { get; }
        public IRelayCommand CreateFolderCommand { get; }
        public IRelayCommand CreateFileCommand { get; }
        public IRelayCommand DeleteCommand { get; }
        public IRelayCommand SaveCommand { get; }
        public IRelayCommand OpenCommand { get; }
        public IRelayCommand StartRenameCommand { get; }
        public IRelayCommand ConfirmRenameCommand { get; }
        public IRelayCommand CancelRenameCommand { get; }

        public FileSystemManagerViewModel(string workspacePath, IMessageBoxService messageBoxService)
        {
            _workspacePath = workspacePath;
            _messageBoxService = messageBoxService;

            RefreshCommand = new RelayCommand(LoadTree);
            CreateFolderCommand = new RelayCommand(CreateFolder);
            CreateFileCommand = new RelayCommand(CreateFile);
            DeleteCommand = new RelayCommand(DeleteSelected);
            SaveCommand = new RelayCommand(SaveCurrent, () => IsDirty && File.Exists(CurrentFilePath));
            OpenCommand = new RelayCommand(OpenSelected);
            StartRenameCommand = new RelayCommand(StartRename);
            ConfirmRenameCommand = new RelayCommand(ConfirmRename);
            CancelRenameCommand = new RelayCommand(CancelRename);

            _document.Changed += (_, __) => IsDirty = true;

            LoadTree();
        }

        private void LoadTree()
        {
            Items.Clear();
            var root = BuildNode(_workspacePath, isRoot: true);
            Items.Add(root);
        }

        private FileNode BuildNode(string path, bool isRoot = false)
        {
            var node = new FileNode
            {
                Name = isRoot ? Path.GetFileName(path.TrimEnd(Path.DirectorySeparatorChar)) : Path.GetFileName(path),
                FullPath = path,
                IsFolder = Directory.Exists(path),
                IsRoot = isRoot,
                Icon = Directory.Exists(path) ? SymbolRegular.Folder20 : GetIcon(path)
            };

            if (node.IsFolder)
            {
                try
                {
                    foreach (var dir in Directory.GetDirectories(path))
                    {
                        var child = BuildNode(dir);
                        child.Parent = node;
                        node.Children.Add(child);
                    }
                    foreach (var file in Directory.GetFiles(path))
                    {
                        var child = BuildNode(file);
                        child.Parent = node;
                        node.Children.Add(child);
                    }
                }
                catch { /* ignore IO exceptions */ }
            }
            return node;
        }

        partial void OnSelectedNodeChanged(FileNode? value)
        {
            if (value == null)
                return;

            if (!PromptSaveIfDirty())
                return; // canceled

            if (!value.IsFolder && File.Exists(value.FullPath))
            {
                try
                {
                    var text = File.ReadAllText(value.FullPath);
                    Document = new TextDocument(text);
                    Document.Changed += (_, __) => IsDirty = true;
                    CurrentFilePath = value.FullPath;
                    IsDirty = false;
                }
                catch (Exception ex)
                {
                    _messageBoxService.Show($"Failed to open file: {ex.Message}");
                }
            }
        }

        private bool PromptSaveIfDirty()
        {
            if (!IsDirty)
                return true;

            var save = _messageBoxService.Confirm("You have unsaved changes. Save now?", "Unsaved changes");
            if (!save)
            {
                // discard
                IsDirty = false;
                return true;
            }

            SaveCurrent();
            return !IsDirty;
        }

        private void CreateFolder()
        {
            var parent = SelectedNode?.IsFolder == true ? SelectedNode.FullPath : SelectedNode?.Parent?.FullPath;
            parent ??= _workspacePath;

            var baseName = "New Folder";
            var target = GetUniqueName(parent, baseName, isFolder: true);
            try
            {
                Directory.CreateDirectory(target);
                LoadTree();
            }
            catch (Exception ex)
            {
                _messageBoxService.Show($"Failed to create folder: {ex.Message}");
            }
        }

        private void CreateFile()
        {
            var parent = SelectedNode?.IsFolder == true ? SelectedNode.FullPath : SelectedNode?.Parent?.FullPath;
            parent ??= _workspacePath;

            var baseName = "NewFile.txt";
            var target = Path.Combine(parent, baseName);
            target = GetUniqueFileName(target);
            try
            {
                File.WriteAllText(target, string.Empty);
                LoadTree();
            }
            catch (Exception ex)
            {
                _messageBoxService.Show($"Failed to create file: {ex.Message}");
            }
        }

        private void DeleteSelected()
        {
            var node = SelectedNode;
            if (node == null || node.IsRoot)
                return;

            if (!_messageBoxService.Confirm($"Delete '{node.Name}'?", "Confirm delete"))
                return;

            try
            {
                if (node.IsFolder)
                    Directory.Delete(node.FullPath, true);
                else
                    File.Delete(node.FullPath);
                LoadTree();
            }
            catch (Exception ex)
            {
                _messageBoxService.Show($"Failed to delete: {ex.Message}");
            }
        }

        private void SaveCurrent()
        {
            if (string.IsNullOrWhiteSpace(CurrentFilePath))
                return;

            try
            {
                File.WriteAllText(CurrentFilePath, Document.Text);
                IsDirty = false;
            }
            catch (Exception ex)
            {
                _messageBoxService.Show($"Failed to save: {ex.Message}");
            }
        }

        private void OpenSelected()
        {
            if (SelectedNode != null && !SelectedNode.IsFolder)
            {
                OnSelectedNodeChanged(SelectedNode);
            }
        }

        private void StartRename()
        {
            if (SelectedNode == null || SelectedNode.IsRoot)
                return;
            SelectedNode.EditingName = SelectedNode.Name;
            SelectedNode.IsEditing = true;
        }

        private void ConfirmRename()
        {
            var node = SelectedNode;
            if (node == null || node.IsRoot)
                return;
            var newName = (node.EditingName ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(newName) || newName == node.Name)
            {
                node.IsEditing = false;
                return;
            }
            var parentDir = Path.GetDirectoryName(node.FullPath)!;
            var targetPath = Path.Combine(parentDir, newName);
            try
            {
                // if editing file currently open, close first
                var reopen = false;
                if (!node.IsFolder && string.Equals(CurrentFilePath, node.FullPath, StringComparison.OrdinalIgnoreCase))
                {
                    reopen = true;
                    CurrentFilePath = string.Empty;
                    Document = new TextDocument();
                    IsDirty = false;
                }
                if (node.IsFolder)
                    Directory.Move(node.FullPath, targetPath);
                else
                    File.Move(node.FullPath, targetPath);
                node.FullPath = targetPath;
                node.Name = newName;
                node.IsEditing = false;
                if (reopen)
                {
                    SelectedNode = node; // triggers open
                }
            }
            catch (Exception ex)
            {
                _messageBoxService.Show($"Failed to rename: {ex.Message}");
            }
        }

        private void CancelRename()
        {
            if (SelectedNode == null) return;
            SelectedNode.IsEditing = false;
        }

        private static string GetUniqueName(string parent, string baseName, bool isFolder)
        {
            var name = baseName;
            var path = Path.Combine(parent, name);
            int i = 1;
            while (Directory.Exists(path) || File.Exists(path))
            {
                name = $"{baseName} ({i++})";
                path = Path.Combine(parent, name);
            }
            return path;
        }

        private static string GetUniqueFileName(string path)
        {
            if (!File.Exists(path)) return path;
            var dir = Path.GetDirectoryName(path)!;
            var file = Path.GetFileNameWithoutExtension(path);
            var ext = Path.GetExtension(path);
            int i = 1;
            string candidate;
            do
            {
                candidate = Path.Combine(dir, $"{file} ({i++}){ext}");
            } while (File.Exists(candidate));
            return candidate;
        }

        private static SymbolRegular GetIcon(string path)
        {
            var ext = Path.GetExtension(path).ToLowerInvariant();
            return ext switch
            {
                ".txt" => SymbolRegular.TextCaseLowercase16,
                ".cs" => SymbolRegular.CodeCs16,
                ".json" => SymbolRegular.Braces16,
                ".js" => SymbolRegular.CodeJs16,
                ".ts" => SymbolRegular.CodeTs16,
                ".rb" => SymbolRegular.CodeRb16,
                ".vba" => SymbolRegular.CodeVb16,
                ".xml" => SymbolRegular.Code16,
                ".py" => SymbolRegular.CodePy16,
                ".md" => SymbolRegular.Markdown20,
                ".html" or ".htm" => SymbolRegular.BookGlobe20,
                "css" => SymbolRegular.DocumentCss16,
                ".jpg" or ".jpeg" or ".png" or ".gif" or ".bmp" or ".svg" => SymbolRegular.Image16,
                _ => SymbolRegular.Document20,
            };
        }
    }
}