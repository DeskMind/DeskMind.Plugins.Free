using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using DeskMind.Core.UI;
using DeskMind.Plugin.PythonRunner.Services;

using ICSharpCode.AvalonEdit.Document;

using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Text.Json;

namespace DeskMind.Plugin.PythonRunner.WPF.ViewModels
{
    public class PythonRunnerViewModel : ObservableObject
    {
        private readonly PythonRunnerPlugin _plugin;
        private readonly string _scriptRoot;
        private readonly IMessageBoxService _messageBoxService;

        private ObservableCollection<ScriptFolder> _scriptFolders = new();

        public ObservableCollection<ScriptFolder> ScriptFolders
        {
            get => _scriptFolders;
            set => SetProperty(ref _scriptFolders, value);
        }

        private ScriptFolder? _selectedFolder;

        public ScriptFolder? SelectedFolder
        {
            get => _selectedFolder;
            set
            {
                if (SetProperty(ref _selectedFolder, value))
                {
                    OnSelectedFolderChanged(value);
                }
            }
        }

        private TextDocument _scriptDocument = new TextDocument();

        public TextDocument ScriptDocument
        {
            get => _scriptDocument;
            set => SetProperty(ref _scriptDocument, value);
        }

        private string _requirementsText = string.Empty;

        public string RequirementsText
        {
            get => _requirementsText;
            set => SetProperty(ref _requirementsText, value);
        }

        private ScriptMetadata _metadata = new ScriptMetadata();

        public ScriptMetadata Metadata
        {
            get => _metadata;
            set => SetProperty(ref _metadata, value);
        }

        // Commands
        public IRelayCommand LoadFoldersCommand { get; }

        public IRelayCommand AddScriptFolderCommand { get; }
        public IRelayCommand RemoveScriptFolderCommand { get; }
        public IRelayCommand SaveScriptCommand { get; }
        public IRelayCommand ValidateScriptCommand { get; }
        public IRelayCommand RunScriptCommand { get; }
        public IRelayCommand SaveRequirementsCommand { get; }
        public IRelayCommand GenerateRequirementsCommand { get; }
        public IRelayCommand AddParameterCommand { get; }
        public IRelayCommand<ScriptParameter> RemoveParameterCommand { get; }
        public IRelayCommand SaveMetadataCommand { get; }

        public PythonRunnerViewModel(PythonRunnerPlugin plugin, string scriptFolder, IMessageBoxService messageBoxService)
        {
            _messageBoxService = messageBoxService;
            _plugin = plugin;
            _scriptRoot = scriptFolder;

            LoadFoldersCommand = new RelayCommand(LoadFolders);
            AddScriptFolderCommand = new RelayCommand(AddScriptFolder);
            RemoveScriptFolderCommand = new RelayCommand(RemoveScriptFolder);
            SaveScriptCommand = new RelayCommand(SaveScript);
            ValidateScriptCommand = new RelayCommand(async () => await ValidateScriptAsync());
            RunScriptCommand = new RelayCommand(async () => await RunScriptAsync());
            SaveRequirementsCommand = new RelayCommand(SaveRequirements);
            GenerateRequirementsCommand = new RelayCommand(async () => await GenerateRequirementsAsync());
            AddParameterCommand = new RelayCommand(AddParameter);
            RemoveParameterCommand = new RelayCommand<ScriptParameter>(RemoveParameter);
            SaveMetadataCommand = new RelayCommand(SaveMetadata);

            LoadFolders();
        }

        public void LoadFolders()
        {
            ScriptFolders.Clear();
            if (!Directory.Exists(_scriptRoot))
                Directory.CreateDirectory(_scriptRoot);

            foreach (var dir in Directory.GetDirectories(_scriptRoot))
            {
                var folder = ScriptFolder.FromDirectory(dir);
                ScriptFolders.Add(folder);
            }

            if (ScriptFolders.Count > 0 && SelectedFolder == null)
                SelectedFolder = ScriptFolders[0];
        }

        private void OnSelectedFolderChanged(ScriptFolder? value)
        {
            if (value == null) return;
            try
            {
                var scriptPath = Path.Combine(value.Path, "script.py");
                var reqPath = Path.Combine(value.Path, "requirements.txt");
                var metaPath = Path.Combine(value.Path, "metadata.json");

                if (File.Exists(scriptPath))
                    ScriptDocument = new TextDocument(File.ReadAllText(scriptPath));
                else
                    ScriptDocument = new TextDocument("# New script template\n");

                RequirementsText = File.Exists(reqPath)
                    ? File.ReadAllText(reqPath)
                    : "# requirements for this script\n";

                Metadata = File.Exists(metaPath)
                    ? ScriptMetadata.Load(metaPath)
                    : ScriptMetadata.CreateDefault(Path.GetFileName(value.Path));
            }
            catch { }
        }

        public void AddScriptFolder()
        {
            var id = System.Guid.NewGuid().ToString();
            var dir = Path.Combine(_scriptRoot, id);
            Directory.CreateDirectory(dir);

            var scriptFile = Path.Combine(dir, "script.py");
            File.WriteAllText(scriptFile, "# New script template\n");
            var reqFile = Path.Combine(dir, "requirements.txt");
            File.WriteAllText(reqFile, "# requirements for this script\n");
            var meta = ScriptMetadata.CreateDefault(id);
            meta.Save(Path.Combine(dir, "metadata.json"));

            var folder = ScriptFolder.FromDirectory(dir);
            ScriptFolders.Add(folder);
            SelectedFolder = folder;
        }

        public void RemoveScriptFolder()
        {
            if (SelectedFolder == null) return;
            var dir = SelectedFolder.Path;
            if (Directory.Exists(dir))
            {
                try { Directory.Delete(dir, true); } catch { }
            }
            ScriptFolders.Remove(SelectedFolder);
            SelectedFolder = ScriptFolders.FirstOrDefault();
        }

        public void SaveScript()
        {
            if (SelectedFolder == null) return;
            var scriptPath = Path.Combine(SelectedFolder.Path, "script.py");
            File.WriteAllText(scriptPath, ScriptDocument.Text);
            _messageBoxService.Show("Script saved.");
        }

        public async Task ValidateScriptAsync()
        {
            if (SelectedFolder == null) return;
            var code = ScriptDocument.Text;
            var response = PythonScriptValidator.Validate(code, out var message);
            var msg = response == true ? "Script is valid!" : string.Join("\n", response);
            _messageBoxService.Show(msg, "Validation");
            await Task.CompletedTask;
        }

        public async Task RunScriptAsync()
        {
            if (SelectedFolder == null) return;
            var result = await _plugin.RunInlineCode(ScriptDocument.Text, "{}", 30, SelectedFolder.Name);
            _messageBoxService.Show(result, "Run");
        }

        public void SaveRequirements()
        {
            if (SelectedFolder == null) return;
            var reqPath = Path.Combine(SelectedFolder.Path, "requirements.txt");
            File.WriteAllText(reqPath, RequirementsText);
            _messageBoxService.Show("Requirements saved.");
        }

        public async Task GenerateRequirementsAsync()
        {
            if (SelectedFolder == null) return;
            var proc = new PythonProcessManager();
            var result = await proc.RunPythonModuleAsync(
                "pipreqs.pipreqs",
                $"\"{SelectedFolder.Path}\" --force --savepath \"{Path.Combine(SelectedFolder.Path, "requirements.txt")}\" --encoding=utf-8",
                SelectedFolder.Path,
60);
            if (!result.Success)
            {
                _messageBoxService.Show($"pipreqs failed: {result.Error}", "Requirements");
                return;
            }
            RequirementsText = File.ReadAllText(Path.Combine(SelectedFolder.Path, "requirements.txt"));
            _messageBoxService.Show("Requirements generated.");
        }

        public void AddParameter()
        {
            Metadata.Parameters.Add(new ScriptParameter { Name = "param", Type = "string", Description = string.Empty });
        }

        public void RemoveParameter(ScriptParameter? param)
        {
            if (param == null) return;
            Metadata.Parameters.Remove(param);
        }

        public void SaveMetadata()
        {
            if (SelectedFolder == null) return;
            var metaPath = Path.Combine(SelectedFolder.Path, "metadata.json");
            Metadata.Save(metaPath);
            _messageBoxService.Show("Metadata saved.");
        }
    }

    public class ScriptFolder
    {
        public string Name { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;

        public static ScriptFolder FromDirectory(string dir)
        {
            return new ScriptFolder
            {
                Name = System.IO.Path.GetFileName(dir),
                Path = dir
            };
        }
    }

    public class ScriptMetadata
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string OutputType { get; set; } = "object";
        public ObservableCollection<ScriptParameter> Parameters { get; set; } = new();

        public static ScriptMetadata CreateDefault(string name) => new ScriptMetadata { Name = name };

        public static ScriptMetadata Load(string path)
        {
            try
            {
                var json = File.ReadAllText(path, Encoding.UTF8);
                return JsonSerializer.Deserialize<ScriptMetadata>(json) ?? new ScriptMetadata();
            }
            catch { return new ScriptMetadata(); }
        }

        public void Save(string path)
        {
            var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(path, json, Encoding.UTF8);
        }
    }

    public class ScriptParameter
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
}