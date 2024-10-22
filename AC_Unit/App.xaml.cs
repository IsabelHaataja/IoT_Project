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

            services.AddSingleton<ILogger<SqliteContext>, Logger<SqliteContext>>();
            services.AddSingleton<IDatabaseContext, SqliteContext>();

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

        var dbContext = host.Services.GetRequiredService<IDatabaseContext>();

        var deviceTwinManager = new DeviceTwinManager(dbContext);
        // Device Twin management

        // TODO - move out of app.xaml
        var deviceId = "AC-45ffebf0"; 

        var iotHubConnectionString = await dbContext.GetIotHubConnectionStringAsync(); 
        var deviceManager = new DeviceManager(iotHubConnectionString, dbContext);

        var registrationResult = await deviceManager.RegisterDeviceAsync(deviceId);

        if (registrationResult.Succeeded)
        {

            var deviceConnectionString = registrationResult.Result;
      
            // Connect the device to IoT Hub
            var connectionResult = await deviceManager.ConnectToIotHubAsync(deviceConnectionString);
            if (!connectionResult.Succeeded)
            {
                Console.WriteLine($"Failed to connect device: {connectionResult.Error}");
            }
        }
        else
        {
            Console.WriteLine($"Failed to register device: {registrationResult.Error}");
        }

        await deviceTwinManager.InitializeDeviceClientAsync();
        await deviceManager.SetUpDirectMethodHandlersAsync();
        await deviceTwinManager.StartSendingDataAsync();
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
