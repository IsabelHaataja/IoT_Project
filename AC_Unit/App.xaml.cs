using AC_Unit.ViewModels;
using AC_Unit.Views;
using Iot_Recources.Data;
using Iot_Recources.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using System.Windows;

namespace AC_Unit;
public partial class App : Application
{
    private static IHost? host;
    public App()
    {
        // logs to windows event viewer
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Warning()
            .CreateLogger();

        host = Host.CreateDefaultBuilder()
            .ConfigureLogging((context, logging) =>
        {
            logging.AddSerilog();
        })
        .ConfigureServices(services =>
        {
            services.AddLogging(configure => configure.AddConsole());
            services.AddSingleton<IDatabaseContext>(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<SqliteContext>>();
                var directoryPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                return new SqliteContext(logger, () => directoryPath);
            });

            // TODO - add connectionstring to iot device
            services.AddSingleton<IDeviceManager>(new DeviceManager(""));

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
        //base.OnStartup(e);        
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

    protected override async void OnExit(ExitEventArgs e)
    {
        var deviceManager = host!.Services.GetRequiredService<IDeviceManager>();

        using var cts = new CancellationTokenSource();

        try
        {
            await deviceManager.DisconnectAsync(cts.Token);
            await host!.StopAsync(cts.Token);
        }
        catch { }

        base.OnExit(e);
    }
}
