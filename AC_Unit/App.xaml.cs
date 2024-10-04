using AC_Unit.ViewModels;
using AC_Unit.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Windows;

namespace AC_Unit;
public partial class App : Application
{
    private static IHost? host;

    public App()
    {
        host = Host.CreateDefaultBuilder().ConfigureServices(services =>
        {
            services.AddSingleton<MainWindow>();
            services.AddSingleton<MainWindowViewModel>();

            services.AddSingleton<HomeView>();
            services.AddSingleton<HomeViewModel>();

            services.AddSingleton<SettingsView>();
            services.AddSingleton<SettingsViewModel>();

        }).Build();


    }

    protected override async void OnStartup(StartupEventArgs e)
    {         
        base.OnStartup(e);        
        var mainWindow = host!.Services.GetRequiredService<MainWindow>();
        mainWindow.Show();

        using var cts = new CancellationTokenSource();

        try
        {
            await host!.RunAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        base.OnExit(e);
    }
}
