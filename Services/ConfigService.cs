using System.IO;
using System.Text.Json;
using WindowsMaintenanceCenter.Models;

namespace WindowsMaintenanceCenter.Services;

public class ConfigService : ILogger
{
    private readonly string _configPath;
    private AppConfig _config = new();

    public ConfigService()
    {
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        var configDir = Path.Combine(baseDir, "Config");
        Directory.CreateDirectory(configDir);
        _configPath = Path.Combine(configDir, "configuracoes.json");
        Load();
    }

    public AppConfig GetConfig() => _config;

    public void UpdateConfig(Action<AppConfig> updater)
    {
        updater(_config);
        Save();
    }

    public void ResetToDefaults()
    {
        _config = new AppConfig();
        Save();
    }

    public void Log(string message)
    {
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        var logDir = Path.Combine(baseDir, "Logs");
        Directory.CreateDirectory(logDir);
        var file = Path.Combine(logDir, $"log_{DateTime.Now:yyyyMMdd}.txt");
        File.AppendAllText(file, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}\n");
    }

    private void Load()
    {
        if (File.Exists(_configPath))
        {
            try
            {
                var json = File.ReadAllText(_configPath);
                var cfg = JsonSerializer.Deserialize<AppConfig>(json);
                if (cfg != null) _config = cfg;
            }
            catch { _config = new AppConfig(); }
        }
    }

    private void Save()
    {
        try
        {
            var json = JsonSerializer.Serialize(_config, new JsonSerializerOptions { WriteIndented = true });
            var tempPath = _configPath + ".tmp";
            File.WriteAllText(tempPath, json);
            File.Move(tempPath, _configPath, overwrite: true);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ConfigService] Erro ao salvar configuração: {ex.Message}");
        }
    }
}