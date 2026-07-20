using System.Windows;

namespace WindowsMaintenanceCenter.Views;

public partial class TaskProgressWindow : Window
{
    public bool WasCancelled { get; private set; }
    public event Action? CancelRequested;

    public TaskProgressWindow()
    {
        InitializeComponent();
        CancelButton.Click += (_, _) => DoCancel();
        CloseButton.Click += (_, _) =>
        {
            DoCancel();
            Close();
        };
    }

    private void Grid_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        DragMove();
    }

    private void DoCancel()
    {
        if (WasCancelled) return;
        WasCancelled = true;
        CancelButton.IsEnabled = false;
        CancelButton.Content = "Cancelando...";
        CloseButton.IsEnabled = false;
        CloseButton.Content = "Cancelando...";
        StatusText.Text = "Cancelando...";
        CancelRequested?.Invoke();
    }

    public void SetTaskInfo(string icon, string title, string subtitle)
    {
        TaskIcon.Text = icon;
        TaskTitle.Text = title;
        TaskSubtitle.Text = subtitle;
    }

    public void UpdateProgress(double value, string statusText)
    {
        Dispatcher.Invoke(() =>
        {
            ProgressBar.Value = value;
            ProgressText.Text = $"{(int)value}%";
            StatusText.Text = statusText;
        });
    }

    public void AppendLog(string line)
    {
    }

    public void MarkCompleted(bool success, string message)
    {
        Dispatcher.Invoke(() =>
        {
            ProgressBar.IsIndeterminate = false;
            ProgressBar.Value = success ? 100 : ProgressBar.Value;
            ProgressText.Text = success ? "100%" : ProgressText.Text;
            StatusText.Text = message;
            StatusText.Foreground = success
                ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(39, 174, 96))
                : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(231, 76, 60));

            CancelButton.Visibility = Visibility.Collapsed;
            CloseButton.Visibility = Visibility.Visible;
            CloseButton.IsEnabled = true;
            CloseButton.Content = "Fechar";
        });
    }

    private void Window_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (!WasCancelled && CloseButton.Visibility != Visibility.Visible)
        {
            e.Cancel = true;
        }
    }
}
