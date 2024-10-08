﻿using AsyncAwaitBestPractices;
using CommanderX16Launcher.Validations;
using ReactiveUI;
using ReactiveUI.Validation.Extensions;
using ReactiveUI.Validation.States;
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
            this.RaisePropertyChanged(nameof(ProgramPathIsEmptyOrWhitespace));
            this.RaisePropertyChanged(nameof(ProgramPathIsPRGFile));
            this.RaisePropertyChanged(nameof(ProgramPathIsBasicFile));
            this.RaisePropertyChanged(nameof(RunProgramAutomaticallyEnabled));
            this.RaisePropertyChanged(nameof(RunCommandText));
        } 
    }

    public string RunCommandText {
        get
        {
            if (EmulatorPathValidation.Validate(EmulatorPath) != ValidationState.Valid)
                return "Command to Run";

            var emulatorArguments = GetX16Arguments();
            return GetCommandString(EmulatorPath, emulatorArguments);
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

    public bool RunProgramAutomaticallyEnabled => (ProgramPathIsPRGFile || ProgramPathIsBasicFile) && File.Exists(ProgramPath);

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
        set {
            this.RaiseAndSetIfChanged(ref _joyPadSelected, value);
            this.RaisePropertyChanged(nameof(RunCommandText));
        }
    }

    private readonly string[] _processorModes = ["65c02", "65c816"];
    public string[] ProcessorModes { get => _processorModes; }

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

    public bool ProgramPathIsEmptyOrWhitespace {
        get => string.IsNullOrWhiteSpace(ProgramPath);
    }

    public bool ProgramPathIsPRGFile {
        get => !ProgramPathIsEmptyOrWhitespace && ProgramPath.EndsWith(".prg", StringComparison.InvariantCultureIgnoreCase);
    }

    public bool ProgramPathIsBasicFile {
        get => !ProgramPathIsEmptyOrWhitespace && (ProgramPath.EndsWith(".bas", StringComparison.InvariantCultureIgnoreCase) || ProgramPath.EndsWith(".txt", StringComparison.InvariantCultureIgnoreCase));
    }

    private string _prgLoadAddress = "";
    public string PRGLoadAddress {
        get => _prgLoadAddress;
        set {
            this.RaiseAndSetIfChanged(ref _prgLoadAddress, value);
            this.RaisePropertyChanged(nameof(RunCommandText));
        }
    }

    public bool ProcessorMode65c02Selected {
        get => ProccessorModeSelected?.Equals("65c02", StringComparison.InvariantCultureIgnoreCase) ?? false;
    }

    private readonly ICommand? _launchEmulatorCommand;
    public ICommand? LaunchEmulatorCommand { get => _launchEmulatorCommand; }

    private readonly ICommand? _browseEmulatorPathCommand;
    public ICommand? BrowseEmulatorPathCommand { get => _browseEmulatorPathCommand; }

    private readonly ICommand? _browseProgramPathCommand;
    public ICommand? BrowseProgramPathCommand { get => _browseProgramPathCommand; }

    public MainWindowViewModel()
    {
        _selectEmulatorFileInteraction = new Interaction<string?, string?>();
        _selectProgramFileInteraction = new Interaction<string?, string?>();

        SetupValidations();

        SetupCommands(ref _launchEmulatorCommand, ref _browseEmulatorPathCommand, ref _browseProgramPathCommand);
    }

    #region "Validations"

    private void SetupValidations() {
        this.ValidationRule(
            viewModel => viewModel.EmulatorPath,
            this.WhenAnyValue(viewModel => viewModel.EmulatorPath, EmulatorPathValidation.ValidateEmpty));

        this.ValidationRule(
            viewModel => viewModel.DebuggerAddress,
            this.WhenAnyValue(vm => vm.DebuggerAddress, vm => vm.EnableDebugMode, HexadecimalTextValidation.Validate));

        this.ValidationRule(
            viewModel => viewModel.PRGLoadAddress,
            this.WhenAnyValue(vm => vm.PRGLoadAddress, vm => vm.ProgramPathIsPRGFile, HexadecimalTextValidation.Validate));
    }

    #endregion

    private void SetupCommands(ref ICommand? launchEmulatorCommand, ref ICommand? browseEmulatorPathCommand, ref ICommand? browseProgramPathCommand) {

        launchEmulatorCommand = ReactiveCommand.Create(
            LaunchEmulatorCommandExecute,
            this.WhenAnyValue(x => x.HasErrors, x => x.EmulatorPath, (errors, emulatorPath) => EmulatorPathValidation.Validate(emulatorPath) == ValidationState.Valid && !errors));

        browseEmulatorPathCommand = ReactiveCommand.CreateFromTask(BrowseEmulatorPathCommandExecute);
        browseProgramPathCommand = ReactiveCommand.CreateFromTask(BrowseProgramPathCommandExecute);
    }

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

        if (!string.IsNullOrWhiteSpace(ProgramPath) && File.Exists(ProgramPath)) {
            var fileInfo = new FileInfo(ProgramPath);
            var fileExt = fileInfo.Extension;

            switch (fileExt) {
                case ".prg":
                    var prgArgument = $"-prg \"{ProgramPath}\"";

                    if (!string.IsNullOrWhiteSpace(PRGLoadAddress))
                        prgArgument = $"{prgArgument},{PRGLoadAddress}";

                    emulatorArguments.Add(prgArgument);

                    break;

                case ".crt":
                    emulatorArguments.Add($"-cart \"{ProgramPath}\"");
                    break;

                case ".bin":
                    emulatorArguments.Add($"-cartbin \"{ProgramPath}\"");
                    break;

                case ".txt":
                case ".bas":
                    emulatorArguments.Add($"-bas \"{ProgramPath}\"");
                    break;
            };

            if (RunProgramAutomatically && (ProgramPathIsPRGFile || ProgramPathIsBasicFile))
                emulatorArguments.Add("-run");
        }

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
