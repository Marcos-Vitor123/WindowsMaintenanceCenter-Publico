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
    private readonly LoggingService _logger;
    private ObservableCollection<HistoryEntry> _entries = new();

    public ObservableCollection<HistoryEntry> Entries
    {
        get => _entries;
        set => SetProperty(ref _entries, value);
    }

    public ICommand RefreshCommand { get; }
    public ICommand ClearCommand { get; }
    public ICommand DeleteEntryCommand { get; }
    public ICommand DeleteSelectedCommand { get; }

    public HistoryViewModel(HistoryService historyService, NotificationService notificationService, LoggingService logger)
    {
        _historyService = historyService;
        _notificationService = notificationService;
        _logger = logger;
        RefreshCommand = new RelayCommand(LoadEntries);
        ClearCommand = new RelayCommand(Clear);
        DeleteEntryCommand = new RelayCommand<HistoryEntry>(DeleteEntry);
        DeleteSelectedCommand = new RelayCommand(DeleteSelected, () => Entries.Any(e => e.IsSelected));
        _logger.Info("[HistoryViewModel] Inicializando, carregando entradas...");
        LoadEntries();
    }

    private void LoadEntries()
    {
        foreach (var entry in Entries)
            entry.PropertyChanged -= OnEntryPropertyChanged;

        Entries.Clear();
        var items = _historyService.Entries;
        _logger.Info($"[HistoryViewModel] LoadEntries: {items.Count} entradas encontradas no HistoryService");
        foreach (var entry in items)
        {
            entry.PropertyChanged += OnEntryPropertyChanged;
            Entries.Add(entry);
            _logger.Info($"[HistoryViewModel]   -> Id={entry.Id} Nome={entry.FunctionName} Sucesso={entry.Success}");
        }
        _logger.Info($"[HistoryViewModel] Entries ObservableCollection agora tem {Entries.Count} itens");
    }

    private void OnEntryPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(HistoryEntry.IsSelected))
            ((RelayCommand)DeleteSelectedCommand).RaiseCanExecuteChanged();
    }

    private void DeleteEntry(HistoryEntry? entry)
    {
        if (entry == null)
        {
            _logger.Warn("[HistoryViewModel] DeleteEntry chamado com entry=null");
            return;
        }

        _logger.Info($"[HistoryViewModel] DeleteEntry solicitado: Id={entry.Id} Nome={entry.FunctionName}");

        var result = MessageBox.Show(
            $"Deseja excluir este registro?\n\n" +
            $"Função: {entry.FunctionName}\n" +
            $"Data: {entry.Date:dd/MM/yyyy HH:mm:ss}",
            "Confirmar Exclusão",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        _logger.Info($"[HistoryViewModel] Resposta do MessageBox: {result}");

        if (result != MessageBoxResult.Yes)
        {
            _logger.Info("[HistoryViewModel] Exclusão cancelada pelo usuário");
            return;
        }

        var deleted = _historyService.DeleteEntry(entry.Id);
        _logger.Info($"[HistoryViewModel] HistoryService.DeleteEntry({entry.Id}) retornou {deleted}");

        entry.PropertyChanged -= OnEntryPropertyChanged;
        var removed = Entries.Remove(entry);
        _logger.Info($"[HistoryViewModel] Entries.Remove(entry) retornou {removed}, Entries.Count agora é {Entries.Count}");

        _notificationService.ShowInfo("Histórico", "Registro excluído com sucesso.");
        _logger.Info("[HistoryViewModel] Exclusão concluída com sucesso");
    }

    private void DeleteSelected()
    {
        var selected = Entries.Where(e => e.IsSelected).ToList();
        if (selected.Count == 0) return;

        var result = MessageBox.Show(
            $"Deseja excluir {selected.Count} registro(s) selecionado(s)?",
            "Confirmar Exclusão em Lote",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result != MessageBoxResult.Yes) return;

        _logger.Info($"[HistoryViewModel] Excluindo {selected.Count} registros selecionados");
        foreach (var entry in selected)
        {
            entry.PropertyChanged -= OnEntryPropertyChanged;
            _historyService.DeleteEntry(entry.Id);
            Entries.Remove(entry);
        }

        ((RelayCommand)DeleteSelectedCommand).RaiseCanExecuteChanged();
        _notificationService.ShowInfo("Histórico", $"{selected.Count} registro(s) excluído(s).");
        _logger.Info($"[HistoryViewModel] {selected.Count} registros excluídos com sucesso");
    }

    private void Clear()
    {
        _logger.Info("[HistoryViewModel] Limpar todo o histórico");
        foreach (var entry in Entries)
            entry.PropertyChanged -= OnEntryPropertyChanged;
        _historyService.Clear();
        LoadEntries();
    }
}
