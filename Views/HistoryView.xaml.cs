using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using WindowsMaintenanceCenter.Models;
using WindowsMaintenanceCenter.ViewModels;

namespace WindowsMaintenanceCenter.Views
{
    public partial class HistoryView : UserControl
    {
        public HistoryView()
        {
            InitializeComponent();
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is HistoryViewModel vm)
            {
                var entry = FindEntry(e.OriginalSource as DependencyObject);
                if (entry != null)
                    vm.DeleteEntryCommand.Execute(entry);
            }
        }

        private HistoryEntry? FindEntry(DependencyObject? source)
        {
            while (source != null)
            {
                if (source is ContentPresenter cp && cp.DataContext is HistoryEntry entry)
                    return entry;
                source = VisualTreeHelper.GetParent(source);
            }
            return null;
        }
    }
}