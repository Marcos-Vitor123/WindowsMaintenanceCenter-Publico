using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Windows.Input;
using WindowsMaintenanceCenter.Models;
using WindowsMaintenanceCenter.Services;

namespace WindowsMaintenanceCenter.ViewModels;

public class DriveItem : ViewModelBase
{
    private bool _isSelected;
    public string Letter { get; set; } = "";
    public string Label { get; set; } = "";
    public long TotalSize { get; set; }
    public bool IsSystemDrive { get; set; }
    public bool IsReadOnly { get; set; }

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (IsReadOnly && !value) return;
            SetProperty(ref _isSelected, value);
        }
    }

    public string DisplayText => $"{Letter} — {Label} ({FormatSize(TotalSize)})";

    private static string FormatSize(long bytes)
    {
        string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
        int i = 0;
        double val = bytes;
        while (val >= 1024 && i < suffixes.Length - 1) { val /= 1024; i++; }
        return $"{val:0.#} {suffixes[i]}";
    }
}

public class SettingsViewModel : ViewModelBase
{
    private readonly ConfigService _configService;
    private readonly NotificationService _notificationService;
    private readonly LoggingService _logger;
    private readonly StartupManager _startupManager;
    private AppConfig _config = new();

    public AppConfig Config
    {
        get => _config;
        set => SetProperty(ref _config, value);
    }

    public ObservableCollection<DriveItem> AvailableDrives { get; } = new();

    public ICommand SaveCommand { get; }
    public ICommand ResetDefaultsCommand { get; }

    public SettingsViewModel(ConfigService configService, NotificationService notificationService, LoggingService logger, StartupManager startupManager)
    {
        _configService = configService;
        _notificationService = notificationService;
        _logger = logger;
        _startupManager = startupManager;

        Config = CloneConfig(_configService.GetConfig());

        DiscoverDrives();

        SaveCommand = new RelayCommand(Save);
        ResetDefaultsCommand = new RelayCommand(ResetDefaults);
    }

    private void DiscoverDrives()
    {
        AvailableDrives.Clear();
        var selected = Config.SelectedDrives?.ToHashSet(StringComparer.OrdinalIgnoreCase) ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (selected.Count == 0) selected.Add("C:");

        try
        {
            foreach (var drive in DriveInfo.GetDrives())
            {
                if (drive.DriveType != DriveType.Fixed) continue;
                if (!drive.IsReady) continue;
                if (drive.Name.Length != 3) continue;

                var letter = drive.Name.TrimEnd('\\').ToUpper();
                var item = new DriveItem
                {
                    Letter = letter,
                    Label = string.IsNullOrWhiteSpace(drive.VolumeLabel) ? "Disco" : drive.VolumeLabel,
                    TotalSize = drive.TotalSize,
                    IsSystemDrive = letter == "C:",
                    IsSelected = selected.Contains(letter)
                };

                if (item.IsSystemDrive)
                    item.IsReadOnly = true;

                AvailableDrives.Add(item);
            }
        }
        catch (Exception ex)
        {
            _logger.Warn($"[SettingsViewModel] Erro ao descobrir discos: {ex.Message}");
        }

        if (!AvailableDrives.Any(d => d.IsSystemDrive))
        {
            AvailableDrives.Insert(0, new DriveItem
            {
                Letter = "C:",
                Label = "Disco do Sistema",
                TotalSize = 0,
                IsSystemDrive = true,
                IsSelected = true,
                IsReadOnly = true
            });
        }
    }

    private void Save()
    {
        try
        {
            Config.SelectedDrives = AvailableDrives
                .Where(d => d.IsSelected)
                .Select(d => d.Letter)
                .ToList();

            if (Config.SelectedDrives.Count == 0)
                Config.SelectedDrives.Add("C:");

            var json = JsonSerializer.Serialize(Config);
            var clone = JsonSerializer.Deserialize<AppConfig>(json);
            if (clone == null) return;

            var oldAutoStart = _configService.GetConfig().StartWithWindows;

            _configService.UpdateConfig(c =>
            {
                c.StartWithWindows = clone.StartWithWindows;
                c.MinimizeToTray = clone.MinimizeToTray;
                c.OpenCentered = clone.OpenCentered;
                c.ShowNotifications = clone.ShowNotifications;
                c.AutoMode = clone.AutoMode;
                c.SelectedDrives = clone.SelectedDrives;
                foreach (var kvp in clone.TaskSchedules)
                {
                    if (c.TaskSchedules.ContainsKey(kvp.Key))
                    {
                        c.TaskSchedules[kvp.Key].Enabled = kvp.Value.Enabled;
                        c.TaskSchedules[kvp.Key].IntervalDays = kvp.Value.IntervalDays;
                    }
                }
            });

            if (oldAutoStart != clone.StartWithWindows)
            {
                _startupManager.SetAutoStart(clone.StartWithWindows);
                _logger.Info($"[SettingsViewModel] Auto-start {(clone.StartWithWindows ? "ATIVADO" : "DESATIVADO")} no registro");
            }

            _notificationService.ShowSuccess("Configurações", "Salvas com sucesso!");
            _logger.Info($"[SettingsViewModel] Configurações salvas — discos: {string.Join(", ", Config.SelectedDrives)}");
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
        DiscoverDrives();
        _notificationService.ShowInfo("Configurações", "Restaurado para padrões recomendados.");
        _logger.Info("[SettingsViewModel] Configurações restauradas para padrão");
    }

    private static AppConfig CloneConfig(AppConfig source)
    {
        var json = JsonSerializer.Serialize(source);
        return JsonSerializer.Deserialize<AppConfig>(json) ?? new AppConfig();
    }
}
