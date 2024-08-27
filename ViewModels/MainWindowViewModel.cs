using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using AsyncAwaitBestPractices;
using ReactiveUI;
using ReactiveUI.Validation.Extensions;
using ReactiveUI.Validation.States;

namespace CommanderX16Launcher.ViewModels;


public partial class MainWindowViewModel : ViewModelBase
{
    private readonly Interaction<string?, string?> _selectEmulatorFileInteraction;
    public Interaction<string?, string?> SelectEmulatorFileInteraction => _selectEmulatorFileInteraction;

    private readonly Interaction<string?, string?> _selectProgramFileInteraction;
    public Interaction<string?, string?> SelectProgramFileInteraction => _selectProgramFileInteraction;

    private string _emulatorPath = "";
    public string EmulatorPath {
        get => _emulatorPath;
        set {
            this.RaiseAndSetIfChanged(ref _emulatorPath, value);
            this.RaisePropertyChanged(nameof(RunCommandText));
        }
    }

    private string _programPath = "";
    public string ProgramPath {
        get => _programPath;
        set {
            this.RaiseAndSetIfChanged(ref _programPath, value);
            this.RaisePropertyChanged(nameof(RunCommandText));
        } 
    }

    public string RunCommandText {
        get {
            FrozenSet<string> emulatorArguments = GetX16Arguments();
            return string.IsNullOrWhiteSpace(EmulatorPath) ? "Command to Run" : GetCommandString(EmulatorPath, emulatorArguments);
        }
    }

    private bool _runProgramAutomatically = false;
    public bool RunProgramAutomatically {
        get => _runProgramAutomatically;
        set {
            this.RaiseAndSetIfChanged(ref _runProgramAutomatically, value);
            this.RaisePropertyChanged(nameof(RunCommandText));
        }
    }

    private bool _enableDebugMode = false;
    public bool EnableDebugMode {
        get => _enableDebugMode;
        set {
            this.RaiseAndSetIfChanged(ref _enableDebugMode, value);
            this.RaisePropertyChanged(nameof(RunCommandText));
        }
    }

    private string _debuggerAddress = "";
    public string DebuggerAddress {
        get => _debuggerAddress;
        set {
                this.RaiseAndSetIfChanged(ref _debuggerAddress, value);
                this.RaisePropertyChanged(nameof(RunCommandText));

        }
    }

    private int _joyPadSelected = 0;
    public int JoyPadSelected {
        get => _joyPadSelected;
        private set {
            this.RaiseAndSetIfChanged(ref _joyPadSelected, value);
            this.RaisePropertyChanged(nameof(RunCommandText));
        }
    }

    private string _processorModeSelected = "65c02";
    public string ProccessorModeSelected {
        get => _processorModeSelected;
        set {
            this.RaiseAndSetIfChanged(ref _processorModeSelected, value);
            this.RaisePropertyChanged(nameof(ProcessorMode65c02Selected));
            this.RaisePropertyChanged(nameof(RunCommandText));
        }
    }

    private bool _suppressRockwellWarnings;
    public bool SuppressRockwellWarnings {
        get => _suppressRockwellWarnings;
        set {
            this.RaiseAndSetIfChanged(ref _suppressRockwellWarnings, value);
            this.RaisePropertyChanged(nameof(RunCommandText));
        }
    }

    public bool ProcessorMode65c02Selected {
        get => ProccessorModeSelected.Equals("65c02", StringComparison.InvariantCultureIgnoreCase);
    }            

    private readonly ICommand? _setSelectedJoyPadCommand;
    public ICommand? SetSelectedJoyPadCommand { get => _setSelectedJoyPadCommand; }

    private readonly ICommand? _launchEmulatorCommand;
    public ICommand? LaunchEmulatorCommand { get => _launchEmulatorCommand; }

    private readonly ICommand? _browseEmulatorPathCommand;
    public ICommand? BrowseEmulatorPathCommand { get => _browseEmulatorPathCommand; }

    private readonly ICommand? _browseProgramPathCommand;
    public ICommand? BrowseProgramPathCommand { get => _browseProgramPathCommand; }

    private readonly ICommand? _processorModeSelectedCommand;
    public ICommand? ProcessorModeSelectedCommand { get => _processorModeSelectedCommand; }

    private readonly IObservable<IValidationState>? _emulatorPathIsX16emu;

    private readonly IObservable<IValidationState>? _debuggerAddressIsHexadecimal;

    private readonly IObservable<bool>? LaunchEmulatorCommandCanExecute;

    public MainWindowViewModel()
    {
        _selectEmulatorFileInteraction = new Interaction<string?, string?>();
        _selectProgramFileInteraction = new Interaction<string?, string?>();

        SetupValidations(ref _emulatorPathIsX16emu, ref _debuggerAddressIsHexadecimal);

        LaunchEmulatorCommandCanExecute = this.WhenAnyValue(
            x => x.HasErrors,
            x => x.EmulatorPath,
            (errors, emulatorPath) => !string.IsNullOrWhiteSpace(emulatorPath) && !errors);

        SetupCommands(ref _launchEmulatorCommand, ref _browseEmulatorPathCommand, ref _browseProgramPathCommand,
                      ref _setSelectedJoyPadCommand, ref _processorModeSelectedCommand);
    }

    private void SetupValidations(ref IObservable<IValidationState>? emulatorPathIsX16emu, ref IObservable<IValidationState>? debuggerAddressIsHexadecimal) {
        emulatorPathIsX16emu = this.WhenAnyValue(viewModel => viewModel.EmulatorPath).Select(emulatorPath => {
            if (string.IsNullOrWhiteSpace(emulatorPath))
                return ValidationState.Valid;

            if (File.Exists(EmulatorPath) && (EmulatorPath.EndsWith("x16emu") || EmulatorPath.EndsWith("x16emu.exe")))
                return ValidationState.Valid;

            return new ValidationState(false, "Emulator Path is not the Commander X16 Emulator");
        });

        debuggerAddressIsHexadecimal = this.WhenAnyValue(vm => vm.DebuggerAddress).Select(debuggerAddress => {
            if (EnableDebugMode && !string.IsNullOrWhiteSpace(debuggerAddress) && !debuggerAddress.All(char.IsAsciiHexDigit))
                return new ValidationState(false, "Invalid Hex Address");

            return ValidationState.Valid;
        });

        this.ValidationRule(viewModel => viewModel.EmulatorPath, emulatorPathIsX16emu!);
        this.ValidationRule(viewModel => viewModel.DebuggerAddress, debuggerAddressIsHexadecimal!);
    }

    private void SetupCommands(ref ICommand? launchEmulatorCommand, ref ICommand? browseEmulatorPathCommand,
        ref ICommand? browseProgramPathCommand, ref ICommand? setSelectedJoyPadCommand,
        ref ICommand? processorModeSelectedCommand) {

        launchEmulatorCommand = ReactiveCommand.Create(LaunchEmulatorCommandExecute, LaunchEmulatorCommandCanExecute);
        browseEmulatorPathCommand = ReactiveCommand.CreateFromTask(BrowseEmulatorPathCommandExecute);
        browseProgramPathCommand = ReactiveCommand.CreateFromTask(BrowseProgramPathCommandExecute);
        setSelectedJoyPadCommand = ReactiveCommand.Create<int>(SetSelectedJoyPadCommandExecute);
        processorModeSelectedCommand = ReactiveCommand.Create<string>(ProcessorModeSelectedCommandExecute);
    }

    // Perhaps use Tag to deactivate radio button if clicked while already selected
    private void SetSelectedJoyPadCommandExecute(int selectedJoyPad) => JoyPadSelected = selectedJoyPad;

    private void ProcessorModeSelectedCommandExecute(string processorMode) => ProccessorModeSelected = processorMode;

    private void LaunchEmulatorCommandExecute() {
        FrozenSet<string> emulatorArguments = GetX16Arguments();
        StartEmulator(EmulatorPath, emulatorArguments).SafeFireAndForget(onException: ex => Debug.WriteLine(ex));            
    }

    private static async Task StartEmulator(string emulatorPath, IEnumerable<string> args) {
        await Task.Run(() => {
            ProcessStartInfo emulatorProcessInfo = new() {
                FileName = emulatorPath,
                UseShellExecute = true
            };

            if (args is not null && args.Any())
                emulatorProcessInfo.Arguments = string.Join(' ', args);

            Process emulatorProcess = new() { StartInfo = emulatorProcessInfo };
            emulatorProcess.Start();
        });
    }

    private static string GetCommandString(string emulatorPath, IEnumerable<string> arguments) {
        StringBuilder runCommandBuilder = new($"\"{emulatorPath}\"");

        if (arguments is not null && arguments.Any())
            arguments.ToList().ForEach(arg => runCommandBuilder.Append($" {arg}"));

        return runCommandBuilder.ToString();
    }

    private FrozenSet<string> GetX16Arguments() {
        List<string> emulatorArguments = [];

        if (!string.IsNullOrEmpty(ProgramPath) && File.Exists(ProgramPath)) {
            var fileInfo = new FileInfo(ProgramPath);
            var fileExt = fileInfo.Extension;

            switch (fileExt) {
                case ".prg":
                    emulatorArguments.Add($"-prg \"{ProgramPath}\"");
                    break;

                case ".crt":
                    emulatorArguments.Add($"-cart \"{ProgramPath}\"");
                    break;

                case ".txt":
                case ".bas":
                    emulatorArguments.Add($"-bas \"{ProgramPath}\"");
                    break;
            };
        }

        if (RunProgramAutomatically)
            emulatorArguments.Add("-run");

        if (EnableDebugMode) {
            var debugSwitchText = "-debug";

            if (!string.IsNullOrWhiteSpace(DebuggerAddress))
                debugSwitchText = $"{debugSwitchText} {DebuggerAddress}";

            emulatorArguments.Add(debugSwitchText);
        }            

        if (JoyPadSelected > 0 && JoyPadSelected < 5)
            emulatorArguments.Add($"-joy{JoyPadSelected}");

        emulatorArguments.Add(GetProcessorModeSwitch());

        return emulatorArguments.ToFrozenSet();
    }

    private string GetProcessorModeSwitch() {
        string processorModeSwitch;

        if (ProccessorModeSelected.Equals("65c02", StringComparison.InvariantCultureIgnoreCase))
        {
            processorModeSwitch = "-c02";

            if (SuppressRockwellWarnings)
                processorModeSwitch = $"{processorModeSwitch} -rockwell";
        }
        else
            processorModeSwitch = "-c816";

        return processorModeSwitch;
    }

    private async Task BrowseEmulatorPathCommandExecute() {
        EmulatorPath = await SelectEmulatorFileInteraction.Handle("Path to Commander X16 Emulator");
    }

    private async Task BrowseProgramPathCommandExecute() {
        ProgramPath = await SelectProgramFileInteraction.Handle("Path to Program File");
    }
}
