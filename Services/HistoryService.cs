using System.IO;
using System.Text.Json;
using WindowsMaintenanceCenter.Models;

namespace WindowsMaintenanceCenter.Services;

public class HistoryService : ILogger
{
    private readonly string _logPath;
    private readonly LoggingService _logger;
    private readonly List<HistoryEntry> _entries = new();
    private readonly object _lock = new();
    private int _nextId = 1;

    public IReadOnlyList<HistoryEntry> Entries => _entries.AsReadOnly();

    public HistoryService(LoggingService logger)
    {
        _logger = logger;
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        var logDir = Path.Combine(baseDir, "Logs");
        Directory.CreateDirectory(logDir);
        _logPath = Path.Combine(logDir, "historico.json");
        Load();
    }

    public void AddEntry(HistoryEntry entry)
    {
        lock (_lock)
        {
            entry.Id = _nextId++;
            _entries.Insert(0, entry);
            if (_entries.Count > 1000) _entries.RemoveAt(_entries.Count - 1);
            _logger.Info($"[HistoryService] AddEntry: Id={entry.Id} Nome={entry.FunctionName} Sucesso={entry.Success}, total={_entries.Count}");
            Save();
        }
    }

    public bool DeleteEntry(int id)
    {
        lock (_lock)
        {
            _logger.Info($"[HistoryService] DeleteEntry: procurando Id={id}, total antes={_entries.Count}");
            var entry = _entries.FirstOrDefault(e => e.Id == id);
            if (entry == null)
            {
                _logger.Warn($"[HistoryService] DeleteEntry: entry Id={id} NÃO encontrada!");
                return false;
            }
            _entries.Remove(entry);
            _logger.Info($"[HistoryService] DeleteEntry: entry Id={id} '{entry.FunctionName}' removida, total depois={_entries.Count}");
            Save();
            return true;
        }
    }

    public IReadOnlyList<HistoryEntry> GetHistory()
    {
        lock (_lock)
        {
            return _entries.ToList();
        }
    }

    public void Clear() 
    { 
        lock (_lock) 
        { 
            _logger.Info($"[HistoryService] Clear: limpando {_entries.Count} entradas");
            _entries.Clear();
            _nextId = 1;
            Save(); 
        } 
    }

    public void Log(string message)
    {
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        var logDir = Path.Combine(baseDir, "Logs");
        Directory.CreateDirectory(logDir);
        var file = Path.Combine(logDir, $"log_{DateTime.Now:yyyyMMdd}.txt");
        File.AppendAllText(file, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}\n", System.Text.Encoding.UTF8);
    }

    private void Load()
    {
        if (File.Exists(_logPath))
        {
            try
            {
                var json = File.ReadAllText(_logPath);
                var list = JsonSerializer.Deserialize<List<HistoryEntry>>(json);
                if (list != null)
                {
                    foreach (var entry in list)
                    {
                        if (entry.Id == 0)
                            entry.Id = _nextId++;
                        else
                            _nextId = Math.Max(_nextId, entry.Id + 1);
                    }
                    _entries.AddRange(list);
                }
            }
            catch { }
        }
    }

    private void Save()
    {
        try
        {
            var json = JsonSerializer.Serialize(_entries, new JsonSerializerOptions { WriteIndented = true });
            var tempPath = _logPath + ".tmp";
            File.WriteAllText(tempPath, json, System.Text.Encoding.UTF8);
            File.Move(tempPath, _logPath, overwrite: true);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[HistoryService] Erro ao salvar histórico: {ex.Message}");
        }
    }
}
