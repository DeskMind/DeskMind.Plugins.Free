using System.Windows;

using DeskMind.Core.UI;

namespace DeskMind.Plugin.PythonRunner.WPF.Services
{
    public class WpfMessageBoxService : IMessageBoxService
    {
        public void Show(string message, string title = "Info")
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public bool Confirm(string message, string title = "Confirm")
        {
            var res = MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question);
            return res == MessageBoxResult.Yes;
        }
    }
}