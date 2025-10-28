using DeskMind.Core.Plugins;
using DeskMind.Core.UI;
using DeskMind.Plugin.PythonRunner.WPF.ViewModels;
using DeskMind.Plugin.PythonRunner.WPF.Views;

using Wpf.Ui.Controls;

namespace DeskMind.Plugin.PythonRunner.WPF
{
    public class PythonRunnerUIProvider : IPluginUIProvider
    {
        public string PluginName => "Python Runner";

        public object PluginIcon => new SymbolIcon(SymbolRegular.CodePyRectangle16); // Use default icon

        private PythonRunnerViewModel? _viewModel;

        public object CreateControl(SecurePluginFactoryBase factory)
        {
            // Build a single VM instance and reuse it across navigations
            if (_viewModel == null)
            {
                // Prefer using the core factory settings to construct the domain plugin
                var pyFactory = factory as DeskMind.Plugin.PythonRunner.PythonRunnerPluginFactory;
                var scriptFolder = pyFactory?.ScriptFolder ?? System.IO.Path.Combine(AppContext.BaseDirectory, "python_scripts");
                var defaultTimeout = pyFactory?.DefaultTimeout ?? 30;

                var domain = new DeskMind.Plugin.PythonRunner.PythonRunnerPlugin(scriptFolder, defaultTimeout);

                IMessageBoxService msg = new Services.WpfMessageBoxService();
                _viewModel = new PythonRunnerViewModel(domain, scriptFolder, msg);
            }

            return new PythonRunnerUIControl
            {
                DataContext = _viewModel
            };
        }
    }
}