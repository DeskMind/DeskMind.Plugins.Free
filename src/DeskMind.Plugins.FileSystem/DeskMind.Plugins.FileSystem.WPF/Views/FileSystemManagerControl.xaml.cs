using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

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

        private void TreeViewItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (Vm?.SelectedNode == null || Vm.SelectedNode.IsRoot)
                return;
            Vm.StartRenameCommand.Execute(null);
        }

        private void RenameTextBox_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox tb)
            {
                tb.Focus();
                tb.SelectAll();
            }
        }

        private void RenameTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            Vm?.ConfirmRenameCommand.Execute(null);
        }

        private void RenameTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                Vm?.ConfirmRenameCommand.Execute(null);
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                Vm?.CancelRenameCommand.Execute(null);
                e.Handled = true;
            }
        }
    }
}