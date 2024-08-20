using System;
using System.Linq;
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

        var emulatorFile = new FilePickerFileType("Only Commander X16 Emulator") {
            Patterns = ["x16emu", "x16emu.exe" ],
            AppleUniformTypeIdentifiers = null,
            MimeTypes = null
        };

        var storageFiles = await topLevel!.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions() {
            AllowMultiple = false,
            Title = context.Input,
            FileTypeFilter = [ emulatorFile, FilePickerFileTypes.All ]
        });

        context.SetOutput(storageFiles?.Select(x => x.TryGetLocalPath()).FirstOrDefault());
    }

    private async Task OpenProgramFilePickerInteractionHandler(IInteractionContext<string?, string?> context)
    {
        // Get our parent top level control in order to get the needed service (in our sample the storage provider. Can also be the clipboard etc.)
        var topLevel = GetTopLevel(this);

        var programFile = new FilePickerFileType("Program Files") {
            Patterns = ["*.prg"],
            AppleUniformTypeIdentifiers = null,
            MimeTypes = null
        };

        var cartridgeFile = new FilePickerFileType("Cartridge Files") {
            Patterns = ["*.crt"],
            AppleUniformTypeIdentifiers = null,
            MimeTypes = null
        };

        var basicFile = new FilePickerFileType("Basic Files") {
            Patterns = ["*.bas", "*.txt"],
            AppleUniformTypeIdentifiers = null,
            MimeTypes = null
        };

        var storageFiles = await topLevel!.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions() {
            AllowMultiple = false,
            Title = context.Input,
            FileTypeFilter = [programFile, cartridgeFile, basicFile, FilePickerFileTypes.All]
        });

        context.SetOutput(storageFiles?.Select(x => x.TryGetLocalPath()).FirstOrDefault());
    }
}