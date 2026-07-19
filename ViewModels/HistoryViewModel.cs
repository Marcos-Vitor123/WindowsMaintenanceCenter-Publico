using System.Collections.ObjectModel;
using System.Windows.Input;
using WindowsMaintenanceCenter.Models;
using WindowsMaintenanceCenter.Services;

namespace WindowsMaintenanceCenter.ViewModels;

public class HistoryViewModel : ViewModelBase
{
    private readonly HistoryService _historyService;
    private ObservableCollection<HistoryEntry> _entries = new();

    public ObservableCollection<HistoryEntry> Entries
    {
        get => _entries;
        set => SetProperty(ref _entries, value);
    }

    public ICommand RefreshCommand { get; }
    public ICommand ClearCommand { get; }
    public ICommand DeleteEntryCommand { get; }

    public HistoryViewModel(HistoryService historyService)
    {
        _historyService = historyService;
        RefreshCommand = new RelayCommand(LoadEntries);
        ClearCommand = new RelayCommand(Clear);
        DeleteEntryCommand = new RelayCommand<HistoryEntry>(DeleteEntry);
        LoadEntries();
    }

    private void LoadEntries()
    {
        Entries.Clear();
        foreach (var entry in _historyService.Entries)
            Entries.Add(entry);
    }

    private void DeleteEntry(HistoryEntry? entry)
    {
        if (entry == null) return;
        _historyService.DeleteEntry(entry.Id);
        Entries.Remove(entry);
    }

    private void Clear()
    {
        _historyService.Clear();
        LoadEntries();
    }
}
