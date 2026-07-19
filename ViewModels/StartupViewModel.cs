using System.Collections.ObjectModel;
using System.Windows.Input;
using WindowsMaintenanceCenter.Models;
using WindowsMaintenanceCenter.Services;

namespace WindowsMaintenanceCenter.ViewModels;

public class StartupViewModel : ViewModelBase
{
    private readonly StartupManager _startupManager;
    private ObservableCollection<StartupEntry> _entries = new();

    public ObservableCollection<StartupEntry> Entries
    {
        get => _entries;
        set => SetProperty(ref _entries, value);
    }

    public ICommand RefreshCommand { get; }
    public ICommand ToggleEnabledCommand { get; }

    public StartupViewModel(StartupManager startupManager)
    {
        _startupManager = startupManager;
        RefreshCommand = new RelayCommand(LoadEntries);
        ToggleEnabledCommand = new RelayCommand<StartupEntry>(ToggleEnabled);
        LoadEntries();
    }

    private void LoadEntries()
    {
        var list = _startupManager.GetStartupEntries();
        Entries.Clear();
        foreach (var entry in list)
            Entries.Add(entry);
    }

    private void ToggleEnabled(StartupEntry? entry)
    {
        if (entry == null) return;
        _startupManager.SetEnabled(entry, !entry.Enabled);
        // Refresh to get actual state
        LoadEntries();
    }
}