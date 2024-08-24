using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Platform.Storage;
using Avalonia.ReactiveUI;
using CommanderX16Launcher.ViewModels;
using ReactiveUI;

namespace CommanderX16Launcher.Views;

public partial class MainWindow : ReactiveWindow<MainWindowViewModel>
{
    public MainWindow()
    {
        InitializeComponent();

        #if DEBUG
            this.AttachDevTools();
        #endif

        this.WhenActivated(d => d(ViewModel!.SelectEmulatorFileInteraction.RegisterHandler(OpenEmulatorFilePickerInteractionHandler)));
        this.WhenActivated(d => d(ViewModel!.SelectProgramFileInteraction.RegisterHandler(OpenProgramFilePickerInteractionHandler)));
    }

    private async Task OpenEmulatorFilePickerInteractionHandler(IInteractionContext<string?, string?> context)
    {
        // Get our parent top level control in order to get the needed service (in our sample the storage provider. Can also be the clipboard etc.)
        var topLevel = GetTopLevel(this);

        FilePickerFileType emulatorFile = new ("Executable Files") {
            AppleUniformTypeIdentifiers = ["public.unix-executable"],
            MimeTypes = ["application/vnd.microsoft.portable-executable", "application/octet-stream"]
        };

        FilePickerOpenOptions filePickerOptions = new () {
            AllowMultiple = false,
            Title = context.Input,
            FileTypeFilter = [ emulatorFile, FilePickerFileTypes.All ],
            SuggestedFileName = "x16emu"
        };

        if (OperatingSystem.IsWindows())
            filePickerOptions.SuggestedFileName = "x16emu.exe";
        
        var storageFiles = await topLevel!.StorageProvider.OpenFilePickerAsync(filePickerOptions);

        context.SetOutput(storageFiles?.Select(x => x.TryGetLocalPath()).FirstOrDefault());
    }

    private async Task OpenProgramFilePickerInteractionHandler(IInteractionContext<string?, string?> context)
    {
        // Get our parent top level control in order to get the needed service (in our sample the storage provider. Can also be the clipboard etc.)
        var topLevel = GetTopLevel(this);

        var programFile = new FilePickerFileType("Program Files") { Patterns = ["*.prg"] };
        var cartridgeFile = new FilePickerFileType("Cartridge Files") { Patterns = ["*.crt"] };

        var basicFile = new FilePickerFileType("Basic Files") {
            Patterns = ["*.bas", "*.txt"],
            AppleUniformTypeIdentifiers = ["public.plain-text"],
            MimeTypes = ["text/plain"]
        };

        var storageFiles = await topLevel!.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions() {
            AllowMultiple = false,
            Title = context.Input,
            FileTypeFilter = [programFile, cartridgeFile, basicFile, FilePickerFileTypes.All]
        });

        context.SetOutput(storageFiles?.Select(x => x.TryGetLocalPath()).FirstOrDefault());
    }
}