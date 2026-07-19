using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media.Animation;
using WindowsMaintenanceCenter.Services;

namespace WindowsMaintenanceCenter.Views
{
    public partial class MainWindow : Window
    {
        private NotificationService? _notificationService;
        private System.Threading.Timer? _notificationTimer;

        public MainWindow()
        {
            InitializeComponent();
        }

        public void SetNotificationService(NotificationService notificationService)
        {
            _notificationService = notificationService;
            _notificationService.ShowNotification += OnShowNotification;
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