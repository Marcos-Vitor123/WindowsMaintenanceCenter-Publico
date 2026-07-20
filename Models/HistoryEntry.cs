using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WindowsMaintenanceCenter.Models;

public class HistoryEntry : INotifyPropertyChanged
{
    private bool _isSelected;

    public int Id { get; set; }
    public DateTime Date { get; set; }
    public string FunctionName { get; set; } = string.Empty;
    public string Command { get; set; } = string.Empty;
    public TimeSpan Duration { get; set; }
    public bool Success { get; set; }
    public int ExitCode { get; set; }
    public long SpaceFreedBytes { get; set; }
    public string? ErrorMessage { get; set; }

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected != value)
            {
                _isSelected = value;
                OnPropertyChanged();
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
