using AC_Unit.ViewModels;
using AC_Unit.Views;
using Iot_Recources.Data;
using Iot_Recources.Services;
using Microsoft.Azure.Devices.Client;
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

            services.AddSingleton<DeviceClient>(sp =>
            {
                var context = sp.GetRequiredService<IDatabaseContext>();
                return Task.Run(async () =>
                {
                    var connectionString = await context.GetDeviceConnectionStringAsync();
                    if (string.IsNullOrEmpty(connectionString))
                    {
                        throw new InvalidOperationException("Device connection string is null or empty.");
                    }
                    return DeviceClient.CreateFromConnectionString(connectionString, TransportType.Mqtt);
                }).GetAwaiter().GetResult();
            });

            services.AddSingleton<IDeviceManager, DeviceManager>();
            services.AddSingleton<IDeviceTwinManager, DeviceTwinManager>();

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

        try
        {
            var dbContext = host.Services.GetRequiredService<IDatabaseContext>();
            var deviceManager = host.Services.GetRequiredService<IDeviceManager>();

            await deviceManager.InitializeDeviceClientAsync();

            var deviceId = "AC-45ffebf0";

            var existingConnectionString = await dbContext.GetDeviceConnectionStringAsync();

            if (string.IsNullOrEmpty(existingConnectionString))
            {
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
            }
            else
            {
                var connectionResult = await deviceManager.ConnectToIotHubAsync(existingConnectionString);
                if (!connectionResult.Succeeded)
                {
                    Console.WriteLine($"Failed to connect device: {connectionResult.Error}");
                }
            }
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
