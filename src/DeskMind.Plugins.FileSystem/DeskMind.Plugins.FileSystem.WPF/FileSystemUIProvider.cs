using DeskMind.Core.Plugins;
using DeskMind.Core.UI;
using DeskMind.Plugins.FileSystem.WPF.ViewModels;
using DeskMind.Plugins.FileSystem.WPF.Views;

using Wpf.Ui.Controls;

using System;
using System.IO;

namespace DeskMind.Plugins.FileSystem.WPF
{
    public class FileSystemUIProvider : IPluginUIProvider
    {
        public string PluginName => "File System";
        public Type TargetPageType => typeof(FileSystemManagerControl);

        public object PluginIcon => new SymbolIcon(SymbolRegular.FolderOpen16);

        private FileSystemManagerViewModel? _viewModel;

        public object CreateControl(SecurePluginFactoryBase factory)
        {
            if (_viewModel == null)
            {
                // Resolve workspace path like the runtime plugin does
                var defaultWorkspace = Path.Combine(AppContext.BaseDirectory, "Workspace");
                var workspacePath = factory.GetConfigValue("WorkspacePath", defaultWorkspace) ?? defaultWorkspace;

                // Ensure it exists so the root node is visible even when empty
                if (!Directory.Exists(workspacePath))
                {
                    Directory.CreateDirectory(workspacePath);
                }

                IMessageBoxService msg = new Services.WpfMessageBoxService();
                _viewModel = new FileSystemManagerViewModel(workspacePath, msg);
            }

            return new FileSystemManagerControl
            {
                DataContext = _viewModel
            };
        }
    }
}