using System.Windows.Input;
using WindowsMaintenanceCenter.Models;
using WindowsMaintenanceCenter.Services;

namespace WindowsMaintenanceCenter.ViewModels;

public class SettingsViewModel : ViewModelBase
{
    private readonly ConfigService _configService;
    private readonly NotificationService _notificationService;
    private AppConfig _config = new();

    public AppConfig Config
    {
        get => _config;
        set => SetProperty(ref _config, value);
    }

    public ICommand SaveCommand { get; }
    public ICommand ResetDefaultsCommand { get; }

    public SettingsViewModel(ConfigService configService, NotificationService notificationService)
    {
        _configService = configService;
        _notificationService = notificationService;
        Config = _configService.GetConfig();

        SaveCommand = new RelayCommand(Save);
        ResetDefaultsCommand = new RelayCommand(ResetDefaults);
    }

    private void Save()
    {
        _configService.UpdateConfig(c =>
        {
            c.StartWithWindows = Config.StartWithWindows;
            c.MinimizeToTray = Config.MinimizeToTray;
            c.OpenCentered = Config.OpenCentered;
            c.ShowNotifications = Config.ShowNotifications;
            c.AutoMode = Config.AutoMode;
            foreach (var kvp in Config.TaskSchedules)
            {
                c.TaskSchedules[kvp.Key].Enabled = kvp.Value.Enabled;
                c.TaskSchedules[kvp.Key].IntervalDays = kvp.Value.IntervalDays;
            }
        });
        _notificationService.ShowSuccess("Configurações", "Salvas com sucesso!");
    }

    private void ResetDefaults()
    {
        _configService.ResetToDefaults();
        Config = _configService.GetConfig();
        _notificationService.ShowInfo("Configurações", "Restaurado para padrões recomendados.");
    }
}