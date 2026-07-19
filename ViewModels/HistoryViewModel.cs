using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using WindowsMaintenanceCenter.Models;
using WindowsMaintenanceCenter.Services;

namespace WindowsMaintenanceCenter.ViewModels;

public class HistoryViewModel : ViewModelBase
{
    private readonly HistoryService _historyService;
    private readonly NotificationService _notificationService;
    private ObservableCollection<HistoryEntry> _entries = new();

    public ObservableCollection<HistoryEntry> Entries
    {
        get => _entries;
        set => SetProperty(ref _entries, value);
    }

    public ICommand RefreshCommand { get; }
    public ICommand ClearCommand { get; }
    public ICommand DeleteEntryCommand { get; }

    public HistoryViewModel(HistoryService historyService, NotificationService notificationService)
    {
        _historyService = historyService;
        _notificationService = notificationService;
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

        var result = MessageBox.Show(
            $"Deseja excluir este registro?\n\n" +
            $"Função: {entry.FunctionName}\n" +
            $"Data: {entry.Date:dd/MM/yyyy HH:mm:ss}",
            "Confirmar Exclusão",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result != MessageBoxResult.Yes) return;

        _historyService.DeleteEntry(entry.Id);
        Entries.Remove(entry);
        _notificationService.ShowInfo("Histórico", "Registro excluído com sucesso.");
    }

    private void Clear()
    {
        _historyService.Clear();
        LoadEntries();
    }
}
