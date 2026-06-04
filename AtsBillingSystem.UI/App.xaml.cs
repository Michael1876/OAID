using System;
using System.IO;
using System.Text.Json;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using AtsBillingSystem.Domain.Interfaces.Infrastructure;
using AtsBillingSystem.Domain.Interfaces.UseCases;
using AtsBillingSystem.Domain.Interfaces.Services;
using AtsBillingSystem.Application.Services;
using AtsBillingSystem.Application.UseCases;
using AtsBillingSystem.Infrastructure.Json;
using AtsBillingSystem.Infrastructure.Json.DependencyInjection;
using AtsBillingSystem.UI.ViewModels;
using AtsBillingSystem.UI.Infrastructure;

namespace AtsBillingSystem.UI
{
    public partial class App : System.Windows.Application
    {
        public static ServiceProvider ServiceProvider { get; private set; } = null!;

        protected override void OnStartup(StartupEventArgs e)
        {
            var jsonApiOptions = LoadJsonApiOptions();

            var services = new ServiceCollection();

            services.AddJsonApiDataLayer(options =>
            {
                options.BaseUrl = jsonApiOptions.BaseUrl;
                options.DataDirectory = jsonApiOptions.DataDirectory;
                options.SimulateNetworkDelayMs = jsonApiOptions.SimulateNetworkDelayMs;
            });

            services.AddScoped<IFileParser, CdrCsvParser>();
            services.AddScoped<IBillingService, BillingService>();
            services.AddScoped<IProcessBillingUseCase, ProcessBillingUseCase>();
            services.AddScoped<IGetSubscribersPagedUseCase, GetSubscribersPagedUseCase>();
            services.AddScoped<IGetCallLogsUseCase, GetCallLogsUseCase>();

            services.AddScoped<IGetActiveTariffsUseCase, GetActiveTariffsUseCase>();

            services.AddSingleton<IDialogService, WpfDialogService>();
            services.AddTransient<BillingProcessingViewModel>();
            services.AddTransient<SubscribersViewModel>();
            services.AddTransient<CallLogViewModel>();

            services.AddTransient<TariffsViewModel>();

            services.AddTransient<MainViewModel>();
            services.AddTransient<Views.MainWindow>();

            ServiceProvider = services.BuildServiceProvider();

            // Окно показываем сразу — без блокирующей загрузки на UI-потоке (иначе deadlock WPF).
            var mainWindow = ServiceProvider.GetRequiredService<Views.MainWindow>();
            mainWindow.DataContext = ServiceProvider.GetRequiredService<MainViewModel>();
            mainWindow.Show();

            base.OnStartup(e);
        }

        private static JsonApiOptions LoadJsonApiOptions()
        {
            var options = new JsonApiOptions();

#if DEBUG
            MergeJsonFile(options, "appsettings.Development.json");
#endif
            MergeJsonFile(options, "appsettings.json");

            return options;
        }

        private static void MergeJsonFile(JsonApiOptions options, string fileName)
        {
            var path = FindSettingsFile(fileName);
            if (path == null || !File.Exists(path))
                return;

            using var doc = JsonDocument.Parse(File.ReadAllText(path));
            if (!doc.RootElement.TryGetProperty("JsonApi", out var jsonApi))
                return;

            if (jsonApi.TryGetProperty("BaseUrl", out var baseUrl))
                options.BaseUrl = baseUrl.GetString() ?? options.BaseUrl;

            if (jsonApi.TryGetProperty("DataDirectory", out var dataDir))
                options.DataDirectory = dataDir.GetString() ?? options.DataDirectory;

            if (jsonApi.TryGetProperty("SimulateNetworkDelayMs", out var delay)
                && delay.TryGetInt32(out var delayMs))
            {
                options.SimulateNetworkDelayMs = delayMs;
            }
        }

        private static string? FindSettingsFile(string fileName)
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null)
            {
                var candidate = Path.Combine(dir.FullName, fileName);
                if (File.Exists(candidate))
                    return candidate;

                dir = dir.Parent;
            }

            return null;
        }
    }
}