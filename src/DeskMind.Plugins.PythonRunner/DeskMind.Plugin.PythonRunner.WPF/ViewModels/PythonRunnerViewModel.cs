using DeskMind.Core.UI;
using DeskMind.Plugin.PythonRunner.Services;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using ICSharpCode.AvalonEdit.Document;

using System.Collections.ObjectModel;
using System.IO;

namespace DeskMind.Plugin.PythonRunner.WPF.ViewModels
{
    public partial class PythonRunnerViewModel : ObservableObject
    {
        private readonly PythonRunnerPlugin _plugin;
        private readonly string _scriptFolder;
        private readonly IMessageBoxService _messageBoxService;

        [ObservableProperty]
        private ObservableCollection<ScriptNode> _scripts = new();

        [ObservableProperty]
        private ScriptNode? _selectedScript;

        [ObservableProperty]
        private TextDocument _document = new TextDocument();

        public PythonRunnerViewModel(PythonRunnerPlugin plugin, string scriptFolder, IMessageBoxService messageBoxService)
        {
            _messageBoxService = messageBoxService;
            _plugin = plugin;
            _scriptFolder = scriptFolder;
            LoadScripts();
        }

        [RelayCommand]
        public void LoadScripts()
        {
            Scripts.Clear();
            if (!Directory.Exists(_scriptFolder))
                Directory.CreateDirectory(_scriptFolder);

            foreach (var dir in Directory.GetDirectories(_scriptFolder))
            {
                var folderNode = new ScriptNode
                {
                    Name = Path.GetFileName(dir),
                    Path = dir,
                    IsFolder = true
                };

                var scriptFile = Path.Combine(dir, "script.py");
                if (File.Exists(scriptFile))
                {
                    folderNode.Children.Add(new ScriptNode
                    {
                        Name = "script.py",
                        Path = scriptFile,
                        IsFolder = false
                    });
                }

                var reqFile = Path.Combine(dir, "requirements.txt");
                if (File.Exists(reqFile))
                {
                    folderNode.Children.Add(new ScriptNode
                    {
                        Name = "requirements.txt",
                        Path = reqFile,
                        IsFolder = false
                    });
                }

                Scripts.Add(folderNode);
            }
        }

        partial void OnSelectedScriptChanged(ScriptNode? value)
        {
            if (value == null) return;
            if (File.Exists(value.Path))
            {
                Document = new TextDocument(File.ReadAllText(value.Path));
            }
        }

        [RelayCommand]
        public void SaveScript()
        {
            if (SelectedScript == null) return;
            File.WriteAllText(SelectedScript.Path, Document.Text);
        }

        [RelayCommand]
        public void AddScript()
        {
            var newId = Guid.NewGuid().ToString();
            var dir = Path.Combine(_scriptFolder, newId);
            Directory.CreateDirectory(dir);

            var scriptFile = Path.Combine(dir, "script.py");
            File.WriteAllText(scriptFile, "# New script template\n");

            var reqFile = Path.Combine(dir, "requirements.txt");
            File.WriteAllText(reqFile, "# requirements for this script\n");

            var folderNode = new ScriptNode
            {
                Name = newId,
                Path = dir,
                IsFolder = true,
                Children = new ObservableCollection<ScriptNode>
        {
            new ScriptNode { Name = "script.py", Path = scriptFile, IsFolder = false },
            new ScriptNode { Name = "requirements.txt", Path = reqFile, IsFolder = false }
        }
            };

            Scripts.Add(folderNode);
        }

        [RelayCommand]
        public void RemoveScript()
        {
            if (SelectedScript == null) return;

            var dir = Path.GetDirectoryName(SelectedScript.Path)!;
            if (Directory.Exists(dir))
                Directory.Delete(dir, true);

            Scripts.Remove(SelectedScript);
            SelectedScript = null;
            Document = new TextDocument();
        }

        [RelayCommand]
        public async Task RunScriptAsync()
        {
            if (SelectedScript == null) return;

            SaveScript(); // ensure latest code

            var argsJson = "{}"; // could bind from UI
            var result = await _plugin.RunScript(
                SelectedScript.Name,
                argsJson,
                30);

            // Show result in a messagebox service
            _messageBoxService.Show(result);
        }

        [RelayCommand]
        public async Task ValidateScriptAsync()
        {
            if (SelectedScript == null) return;
            SaveScript();

            var code = File.ReadAllText(SelectedScript.Path);
            var response = PythonScriptValidator.Validate(code, out var message);

            var msg = response == true ? "Script is valid!" : string.Join("\n", response);
            _messageBoxService.Show(msg, "Validation");
        }

        [RelayCommand]
        public void EditRequirements()
        {
            if (SelectedScript == null) return;

            // If folder is selected, look for requirements.txt inside it
            string requirementsFile;
            if (SelectedScript.IsFolder)
                requirementsFile = Path.Combine(SelectedScript.Path, "requirements.txt");
            else
                requirementsFile = SelectedScript.Name == "requirements.txt"
                    ? SelectedScript.Path
                    : Path.Combine(Path.GetDirectoryName(SelectedScript.Path)!, "requirements.txt");

            if (!File.Exists(requirementsFile))
            {
                File.WriteAllText(requirementsFile, "# requirements for this script\n");
            }

            Document = new TextDocument(File.ReadAllText(requirementsFile));
            SelectedScript = new ScriptNode
            {
                Name = "requirements.txt",
                Path = requirementsFile,
                IsFolder = false
            };
        }
    }

    public class ScriptNode
    {
        public string Name { get; set; } = "";
        public string Path { get; set; } = "";
        public bool IsFolder { get; set; }
        public ObservableCollection<ScriptNode> Children { get; set; } = new();
    }
}

