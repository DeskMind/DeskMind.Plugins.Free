using DeskMind.Core.Plugins;
using DeskMind.Core.UI;
using DeskMind.Plugin.PythonRunner.WPF.ViewModels;
using DeskMind.Plugin.PythonRunner.WPF.Views;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeskMind.Plugin.PythonRunner.WPF
{
    public class PythonRunnerUIProvider : IPluginUIProvider
    {
        public string PluginName => "Python Runner";

        public object CreateControl(SecurePluginFactoryBase factory)
        {
            return new PythonRunnerUIControl
            {/*
                DataContext = new PythonRunnerViewModel(
                    (PythonRunnerPlugin)factory.CreatePlugin(),
                    "scripts"
                )*/
            };
        }
    }
}

