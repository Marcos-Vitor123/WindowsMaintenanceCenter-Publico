using System;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using WindowsMaintenanceCenter.Services;
using WindowsMaintenanceCenter.ViewModels;

namespace WindowsMaintenanceCenter
{
    public partial class App : Application
    {
        public static IServiceProvider? Services { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            var services = new ServiceCollection();

            // Services
            services.AddSingleton<ILogger, ConfigService>();
            services.AddSingleton<ConfigService>();
            services.AddSingleton<HistoryService>();
            services.AddSingleton<DiagnosticService>(sp => new DiagnosticService(sp.GetRequiredService<HistoryService>()));
            services.AddSingleton<NotificationService>();
            services.AddSingleton<SoundService>();
            services.AddSingleton<StartupManager>();
            services.AddSingleton<MaintenanceEngine>();
            services.AddSingleton<SystemRepairEngine>();
            services.AddSingleton<DeepCleanEngine>();
            services.AddSingleton<AutomationService>();

            // ViewModels
            services.AddTransient<MainViewModel>();

            Services = services.BuildServiceProvider();

            var automation = Services.GetRequiredService<AutomationService>();
            automation.Start();

            base.OnStartup(e);

            var mainWindow = new Views.MainWindow();
            mainWindow.DataContext = Services.GetRequiredService<MainViewModel>();
            mainWindow.SetNotificationService(Services.GetRequiredService<NotificationService>());
            mainWindow.Show();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            var automation = Services?.GetService<AutomationService>();
            automation?.Stop();
            base.OnExit(e);
        }
    }
}