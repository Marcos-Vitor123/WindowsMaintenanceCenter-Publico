using System;
using System.Diagnostics;
using System.Windows;

namespace WindowsMaintenanceCenter.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
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