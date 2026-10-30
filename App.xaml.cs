using System.Windows;
using CurrencyConverter_Static.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CurrsencyConverter_Static
{
    public partial class App : Application
    {
        private ServiceProvider? _serviceProvider;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            AppDomain.CurrentDomain.SetData("DataDirectory", AppContext.BaseDirectory);

            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                .Build();

            var connectionString = configuration.GetConnectionString("CurrencyConverter");
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    "Connection string 'CurrencyConverter' is missing from appsettings.json.");
            }

            var services = new ServiceCollection();
            services.AddDbContextFactory<CurrencyConverterDbContext>(options =>
                options.UseSqlServer(connectionString));
            services.AddTransient<CurrencyConverter_Static.MainWindow>();

            _serviceProvider = services.BuildServiceProvider();
            var mainWindow = _serviceProvider.GetRequiredService<CurrencyConverter_Static.MainWindow>();
            MainWindow = mainWindow;
            mainWindow.Show();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _serviceProvider?.Dispose();
            base.OnExit(e);
        }
    }
}
