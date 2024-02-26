using Microsoft.Extensions.Configuration;
using Serilog;
using Splat;
using SwarmNotifier.Configurations;
using SwarmNotifier.Services;

namespace SwarmNotifier
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var configuration = new ConfigurationBuilder()
              .AddJsonFile("appsettings.json")
              .Build();

            RegisterServices(configuration);

            var monitor = Locator.Current.GetService<SwarmMonitor>();
            monitor?.Run().Wait();
        }

        private static void RegisterServices(IConfiguration configuration)
        {
            // Configure Serilog global logger first, binding our configuration values to it
            Log.Logger = new LoggerConfiguration()
              .ReadFrom.Configuration(configuration)
              .CreateLogger();
            Log.Logger.Information("New session");

            var appSettings = new AppSettings();
            configuration.Bind(appSettings);

            // Register our services for DI
            SplatRegistrations.RegisterConstant(appSettings);
            SplatRegistrations.RegisterConstant(appSettings.SwarmConfiguration);
            SplatRegistrations.RegisterConstant(appSettings.SlackConfiguration);
            SplatRegistrations.RegisterConstant(appSettings.SwarmEventConfiguration);
            SplatRegistrations.RegisterLazySingleton<SwarmService>();
            SplatRegistrations.RegisterLazySingleton<SlackHelper>();
            SplatRegistrations.RegisterLazySingleton<SwarmMonitor>();

            // Finalize our registration
            SplatRegistrations.SetupIOC();
        }
    }
}