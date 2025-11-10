using CommunityToolkit.Mvvm.ComponentModel;

using System.Collections.ObjectModel;

using Wpf.Ui.Controls;

namespace DeskMind.Plugins.FileSystem.WPF.Models
{
    public partial class FileNode : ObservableObject
    {
        [ObservableProperty]
        private SymbolRegular _icon = SymbolRegular.Document20;

        [ObservableProperty]
        private string _name = string.Empty;

        [ObservableProperty]
        private string _fullPath = string.Empty;

        [ObservableProperty]
        private bool _isFolder;

        [ObservableProperty]
        private bool _isRoot;

        [ObservableProperty]
        private bool _isEditing;

        [ObservableProperty]
        private string _editingName = string.Empty;

        public FileNode? Parent { get; set; }
        public ObservableCollection<FileNode> Children { get; } = new();
    }
}