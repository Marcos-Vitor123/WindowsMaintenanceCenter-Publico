using WindowsMaintenanceCenter.Models;

namespace WindowsMaintenanceCenter.Services;

public class NotificationService
{
    public event Action<string, string>? ShowNotification; // title, message

    public void Show(string title, string message)
    {
        ShowNotification?.Invoke(title, message);
    }

    public void ShowSuccess(string title, string message)
    {
        ShowNotification?.Invoke($"✅ {title}", message);
    }

    public void ShowWarning(string title, string message)
    {
        ShowNotification?.Invoke($"⚠️ {title}", message);
    }

    public void ShowError(string title, string message)
    {
        ShowNotification?.Invoke($"❌ {title}", message);
    }

    public void ShowInfo(string title, string message)
    {
        ShowNotification?.Invoke($"ℹ️ {title}", message);
    }
}