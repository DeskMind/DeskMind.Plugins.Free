using CommunityToolkit.Mvvm.ComponentModel;

using System.Collections.ObjectModel;

namespace DeskMind.Plugins.FileSystem.WPF.Models
{
    public class FileNode : ObservableObject
    {
        public string Name { get; set; } = string.Empty;
        public string FullPath { get; set; } = string.Empty;
        public bool IsFolder { get; set; }
        public bool IsRoot { get; set; }

        public FileNode? Parent { get; set; }
        public ObservableCollection<FileNode> Children { get; } = new();
    }
}