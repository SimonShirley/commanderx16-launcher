using System.Linq;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using CommanderX16Launcher.Extensions;
using CommanderX16Launcher.ViewModels;
using CommanderX16Launcher.Views;
using Microsoft.Extensions.DependencyInjection;

namespace CommanderX16Launcher;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        // Line below is needed to remove Avalonia data validation.
        // Without this line you will get duplicate validations from both Avalonia and CT
        // BindingPlugins.DataValidators.RemoveAt(0);

        // Get an array of plugins to remove
        var dataValidationPluginsToRemove = BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        // remove each entry found
        foreach (var plugin in dataValidationPluginsToRemove)
            BindingPlugins.DataValidators.Remove(plugin);

        // Register all the services needed for the application to run
        ServiceCollection collection = new();
        collection.AddCommonServices();

        // Creates a ServiceProvider containing services from the provided IServiceCollection
        var services = collection.BuildServiceProvider();

        var viewModel = services.GetRequiredService<MainWindowViewModel>();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow { DataContext = viewModel };
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
        {
            singleViewPlatform.MainView = new MainWindow { DataContext = viewModel };
        }

        base.OnFrameworkInitializationCompleted();
    }
}