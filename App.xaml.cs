using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using WindowsMaintenanceCenter.Services;
using WindowsMaintenanceCenter.ViewModels;

namespace WindowsMaintenanceCenter
{
    public partial class App : Application
    {
        public static IServiceProvider? Services { get; private set; }
        private Mutex? _mutex;

        protected override void OnStartup(StartupEventArgs e)
        {
            _mutex = new Mutex(true, "WindowsMaintenanceCenter_SingleInstance", out bool createdNew);
            if (!createdNew)
            {
                Shutdown();
                return;
            }

            if (!CheckRuntime())
            {
                Shutdown();
                return;
            }

            var services = new ServiceCollection();

            // Services
            services.AddSingleton<ConfigService>();
            services.AddSingleton<ILogger>(sp => sp.GetRequiredService<ConfigService>());
            services.AddSingleton<HistoryService>();
            services.AddSingleton<DiagnosticService>(sp => new DiagnosticService(sp.GetRequiredService<HistoryService>()));
            services.AddSingleton<NotificationService>();
            services.AddSingleton<SoundService>();
            services.AddSingleton<StartupManager>();
            services.AddSingleton<LoggingService>();
            services.AddSingleton<MaintenanceEngine>();
            services.AddSingleton<SystemRepairEngine>();
            services.AddSingleton<DiskCleanupService>();
            services.AddSingleton<DeepCleanEngine>();
            services.AddSingleton<AutomationService>();

            // ViewModels
            services.AddTransient<MainViewModel>();

            Services = services.BuildServiceProvider();

            var logger = Services.GetRequiredService<LoggingService>();
            logger.Info("=== WMC iniciado ===");

            var automation = Services.GetRequiredService<AutomationService>();
            automation.Start();

            base.OnStartup(e);

            var mainWindow = new Views.MainWindow();
            mainWindow.DataContext = Services.GetRequiredService<MainViewModel>();
            mainWindow.SetNotificationService(Services.GetRequiredService<NotificationService>());
            mainWindow.SetConfigService(Services.GetRequiredService<ConfigService>());

            var config = Services.GetRequiredService<ConfigService>().GetConfig();

            if (config.OpenCentered)
            {
                mainWindow.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;
            }
            else
            {
                mainWindow.WindowStartupLocation = System.Windows.WindowStartupLocation.Manual;
                mainWindow.Left = 100;
                mainWindow.Top = 100;
            }

            var startMinimized = e.Args.Contains("--minimized", StringComparer.OrdinalIgnoreCase);

            if (startMinimized)
            {
                mainWindow.SetStartedMinimized(true);
                mainWindow.Show();
                mainWindow.HideToTray();
            }
            else
            {
                mainWindow.Show();
            }

            logger.Info("MainWindow exibida com sucesso");
        }

        protected override void OnExit(ExitEventArgs e)
        {
            var logger = Services?.GetService<LoggingService>();
            logger?.Info("=== WMC encerrado ===");

            var automation = Services?.GetService<AutomationService>();
            automation?.Stop();

            _mutex?.ReleaseMutex();
            _mutex?.Dispose();

            base.OnExit(e);
        }

        private static bool CheckRuntime()
        {
            var description = RuntimeInformation.FrameworkDescription;
            if (description.Contains(".NET 10", StringComparison.OrdinalIgnoreCase))
                return true;

            var result = MessageBox.Show(
                $"Este programa requer o .NET 10.0 Runtime.\n\n" +
                $"Versão detectada: {description}\n\n" +
                $"Deseja abrir a página de download do .NET 10.0?",
                "Runtime Incompatível",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "https://dotnet.microsoft.com/download/dotnet/10.0",
                    UseShellExecute = true
                });
            }

            return false;
        }
    }
}