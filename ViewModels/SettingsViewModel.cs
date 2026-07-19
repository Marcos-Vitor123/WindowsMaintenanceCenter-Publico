using System.Text.Json;
using System.Windows.Input;
using WindowsMaintenanceCenter.Models;
using WindowsMaintenanceCenter.Services;

namespace WindowsMaintenanceCenter.ViewModels;

public class SettingsViewModel : ViewModelBase
{
    private readonly ConfigService _configService;
    private readonly NotificationService _notificationService;
    private readonly LoggingService _logger;
    private AppConfig _config = new();

    public AppConfig Config
    {
        get => _config;
        set => SetProperty(ref _config, value);
    }

    public ICommand SaveCommand { get; }
    public ICommand ResetDefaultsCommand { get; }

    public SettingsViewModel(ConfigService configService, NotificationService notificationService, LoggingService logger)
    {
        _configService = configService;
        _notificationService = notificationService;
        _logger = logger;

        Config = CloneConfig(_configService.GetConfig());

        SaveCommand = new RelayCommand(Save);
        ResetDefaultsCommand = new RelayCommand(ResetDefaults);
    }

    private void Save()
    {
        try
        {
            var json = JsonSerializer.Serialize(Config);
            var clone = JsonSerializer.Deserialize<AppConfig>(json);
            if (clone == null) return;

            _configService.UpdateConfig(c =>
            {
                c.StartWithWindows = clone.StartWithWindows;
                c.MinimizeToTray = clone.MinimizeToTray;
                c.OpenCentered = clone.OpenCentered;
                c.ShowNotifications = clone.ShowNotifications;
                c.AutoMode = clone.AutoMode;
                foreach (var kvp in clone.TaskSchedules)
                {
                    if (c.TaskSchedules.ContainsKey(kvp.Key))
                    {
                        c.TaskSchedules[kvp.Key].Enabled = kvp.Value.Enabled;
                        c.TaskSchedules[kvp.Key].IntervalDays = kvp.Value.IntervalDays;
                    }
                }
            });
            _notificationService.ShowSuccess("Configurações", "Salvas com sucesso!");
            _logger.Info("[SettingsViewModel] Configurações salvas pelo usuário");
        }
        catch (Exception ex)
        {
            _notificationService.ShowError("Configurações", $"Erro ao salvar: {ex.Message}");
            _logger.Error("[SettingsViewModel] Erro ao salvar configurações", ex);
        }
    }

    private void ResetDefaults()
    {
        _configService.ResetToDefaults();
        Config = CloneConfig(_configService.GetConfig());
        _notificationService.ShowInfo("Configurações", "Restaurado para padrões recomendados.");
        _logger.Info("[SettingsViewModel] Configurações restauradas para padrão");
    }

    private static AppConfig CloneConfig(AppConfig source)
    {
        var json = JsonSerializer.Serialize(source);
        return JsonSerializer.Deserialize<AppConfig>(json) ?? new AppConfig();
    }
}
