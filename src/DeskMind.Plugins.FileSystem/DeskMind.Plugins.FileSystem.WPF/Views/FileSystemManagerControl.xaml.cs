using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;

using DeskMind.Plugins.FileSystem.WPF.Models;
using DeskMind.Plugins.FileSystem.WPF.ViewModels;

using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Highlighting;

namespace DeskMind.Plugins.FileSystem.WPF.Views
{
    /// <summary>
    /// Interaction logic for FileSystemManagerControl.xaml
    /// </summary>
    public partial class FileSystemManagerControl : UserControl
    {
        public FileSystemManagerControl()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
        }

        private FileSystemManagerViewModel? Vm => DataContext as FileSystemManagerViewModel;

        private TextEditor? _editor;
        private TextEditor? EditorControl => _editor ??= (TextEditor?)FindName("Editor");

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (Vm != null && EditorControl != null)
            {
                // Bind document manually when VM changes
                EditorControl.Document = Vm.Document;
                Vm.PropertyChanged += (s, args) =>
                {
                    if (args.PropertyName == nameof(FileSystemManagerViewModel.Document))
                    {
                        EditorControl.Document = Vm.Document;
                        ApplyHighlighting(Vm.CurrentFilePath);
                    }
                    else if (args.PropertyName == nameof(FileSystemManagerViewModel.CurrentFilePath))
                    {
                        ApplyHighlighting(Vm.CurrentFilePath);
                    }
                };
                ApplyHighlighting(Vm.CurrentFilePath);
            }
        }

        private void ApplyHighlighting(string path)
        {
            if (EditorControl == null)
                return;
            if (string.IsNullOrEmpty(path))
            {
                EditorControl.SyntaxHighlighting = null;
                return;
            }
            var ext = Path.GetExtension(path).ToLowerInvariant();
            EditorControl.SyntaxHighlighting = HighlightingManager.Instance.GetDefinitionByExtension(ext);
        }

        private void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (Vm == null) return;
            if (e.NewValue is FileNode node)
            {
                Vm.SelectedNode = node;
            }
        }
    }
}