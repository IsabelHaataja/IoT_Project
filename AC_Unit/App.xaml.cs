using Microsoft.Extensions.Hosting;
using System.Configuration;
using System.Data;
using System.Windows;

namespace AC_Unit
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private static IHost? host;

        public App()
        {
            //host = Host.CreateDefaultBuilder().ConfigureServices(services =>
            //{

            //});
        }
    }
}
