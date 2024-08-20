using Microsoft.Extensions.DependencyInjection;
using CommanderX16Launcher.ViewModels;

namespace CommanderX16Launcher.Extensions
{
    public static class ServiceCollectionExtension {
        public static void AddCommonServices(this IServiceCollection collection) {
            collection.AddTransient<MainWindowViewModel>();
        }
    }
}