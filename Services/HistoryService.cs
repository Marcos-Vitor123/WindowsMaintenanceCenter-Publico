using System.IO;
using System.Text.Json;
using WindowsMaintenanceCenter.Models;

namespace WindowsMaintenanceCenter.Services;

public class HistoryService : ILogger
{
    private readonly string _logPath;
    private readonly List<HistoryEntry> _entries = new();
    private readonly object _lock = new();
    private int _nextId = 1;

    public IReadOnlyList<HistoryEntry> Entries => _entries.AsReadOnly();

    public HistoryService()
    {
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
            Save();
        }
    }

    public bool DeleteEntry(int id)
    {
        lock (_lock)
        {
            var entry = _entries.FirstOrDefault(e => e.Id == id);
            if (entry == null) return false;
            _entries.Remove(entry);
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
