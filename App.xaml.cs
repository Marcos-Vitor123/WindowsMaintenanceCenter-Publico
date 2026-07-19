using System;
using System.Collections.Generic;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using WindowsMaintenanceCenter.Models;
using WindowsMaintenanceCenter.Services;
using WindowsMaintenanceCenter.ViewModels;
using WindowsMaintenanceCenter.Views;

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
            services.AddSingleton<DiagnosticService>();
            services.AddSingleton<NotificationService>();
            services.AddSingleton<SoundService>();
            services.AddSingleton<StartupManager>();
            services.AddSingleton<MaintenanceEngine>();
            services.AddSingleton<SystemRepairEngine>();
            services.AddSingleton<DeepCleanEngine>();
            services.AddSingleton<AutomationService>();

            // ViewModels
            services.AddTransient<MainViewModel>();
            services.AddTransient<HomeViewModel>();
            services.AddTransient<MaintenanceViewModel>();
            services.AddTransient<DiagnosticsViewModel>();
            services.AddTransient<StartupViewModel>();
            services.AddTransient<SettingsViewModel>();
            services.AddTransient<HistoryViewModel>();

            // Views
            services.AddTransient<MainWindow>();
            services.AddTransient<HomeView>();
            services.AddTransient<MaintenanceView>();
            services.AddTransient<DiagnosticsView>();
            services.AddTransient<StartupView>();
            services.AddTransient<SettingsView>();
            services.AddTransient<HistoryView>();

            Services = services.BuildServiceProvider();

            var automation = Services.GetRequiredService<AutomationService>();
            automation.Start();

            base.OnStartup(e);

            var mainWindow = Services.GetRequiredService<MainWindow>();
            mainWindow.DataContext = Services.GetRequiredService<MainViewModel>();
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