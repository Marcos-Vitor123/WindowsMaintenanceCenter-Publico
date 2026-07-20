using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using WindowsMaintenanceCenter.Services;

namespace WindowsMaintenanceCenter.Views
{
    public partial class MainWindow : Window
    {
        private NotificationService? _notificationService;
        private System.Threading.Timer? _notificationTimer;
        private System.Windows.Forms.NotifyIcon? _trayIcon;
        private ConfigService? _configService;
        private bool _startedMinimized;

        public MainWindow()
        {
            InitializeComponent();
            SetupTrayIcon();
        }

        public void SetStartedMinimized(bool minimized)
        {
            _startedMinimized = minimized;
        }

        public void SetNotificationService(NotificationService notificationService)
        {
            _notificationService = notificationService;
            _notificationService.ShowNotification += OnShowNotification;
        }

        public void SetConfigService(ConfigService configService)
        {
            _configService = configService;
        }

        private void SetupTrayIcon()
        {
            var iconPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "ico.ico");
            var icon = System.IO.File.Exists(iconPath)
                ? new Icon(iconPath)
                : SystemIcons.Application;

            _trayIcon = new System.Windows.Forms.NotifyIcon
            {
                Icon = icon,
                Text = "Windows Maintenance Center",
                Visible = false
            };

            var menu = new System.Windows.Forms.ContextMenuStrip();
            menu.Items.Add("Abrir", null, (_, _) => Dispatcher.Invoke(ShowFromTray));
            menu.Items.Add("-");
            menu.Items.Add("Sair", null, (_, _) => Dispatcher.Invoke(CloseForced));
            _trayIcon.ContextMenuStrip = menu;

            _trayIcon.DoubleClick += (_, _) => Dispatcher.Invoke(ShowFromTray);
        }

        private void ShowFromTray()
        {
            Show();
            WindowState = WindowState.Normal;
            Activate();
            _trayIcon!.Visible = false;
        }

        public void HideToTray()
        {
            _trayIcon!.Visible = true;
            Hide();
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            if (_configService != null && _configService.GetConfig().MinimizeToTray)
            {
                e.Cancel = true;
                HideToTray();
                return;
            }

            CleanupTray();
            base.OnClosing(e);
        }

        private void CloseForced()
        {
            CleanupTray();
            Application.Current.Shutdown();
        }

        private void CleanupTray()
        {
            if (_trayIcon != null)
            {
                _trayIcon.Visible = false;
                _trayIcon.Dispose();
                _trayIcon = null;
            }
        }

        private void OnShowNotification(string title, string message)
        {
            Dispatcher.Invoke(() =>
            {
                NotificationTitle.Text = title;
                NotificationMessage.Text = message;
                NotificationPanel.Visibility = Visibility.Visible;

                var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(250));
                NotificationPanel.BeginAnimation(OpacityProperty, fadeIn);

                _notificationTimer?.Dispose();
                _notificationTimer = new System.Threading.Timer(_ =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(400));
                        fadeOut.Completed += (_, _) => NotificationPanel.Visibility = Visibility.Collapsed;
                        NotificationPanel.BeginAnimation(OpacityProperty, fadeOut);
                    });
                }, null, 3500, System.Threading.Timeout.Infinite);
            });
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            if (_startedMinimized) return;

            if (!IsRunAsAdministrator())
            {
                MessageBox.Show(
                    "Este programa precisa ser executado como Administrador para funcionar corretamente.\n\n" +
                    "Clique com o botão direito no executável e selecione \"Executar como administrador\".",
                    "Permissão de Administrador Necessária",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        private bool IsRunAsAdministrator()
        {
            var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
            var principal = new System.Security.Principal.WindowsPrincipal(identity);
            return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
        }
    }
}
