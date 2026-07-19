using System.Collections.ObjectModel;
using System.Windows.Input;
using WindowsMaintenanceCenter.Models;
using WindowsMaintenanceCenter.Services;

namespace WindowsMaintenanceCenter.ViewModels;

public class MaintenanceViewModel : ViewModelBase
{
    private readonly MaintenanceEngine _maintenanceEngine;
    private readonly SystemRepairEngine _repairEngine;
    private readonly DeepCleanEngine _deepCleanEngine;
    private readonly NotificationService _notificationService;
    private readonly SoundService _soundService;
    private readonly ConfigService _configService;
    private readonly HistoryService _historyService;
    private readonly LoggingService _logger;
    private readonly Action<string, int, bool, bool>? _statusCallback;
    private bool _isExecuting = false;

    public ObservableCollection<MaintenanceTask> Tasks { get; } = new();

    public ICommand ExecuteTaskCommand { get; }

    public MaintenanceViewModel(
        MaintenanceEngine maintenanceEngine,
        SystemRepairEngine repairEngine,
        DeepCleanEngine deepCleanEngine,
        NotificationService notificationService,
        SoundService soundService,
        ConfigService configService,
        HistoryService historyService,
        LoggingService logger,
        Action<string, int, bool, bool>? statusCallback = null)
    {
        _maintenanceEngine = maintenanceEngine;
        _repairEngine = repairEngine;
        _deepCleanEngine = deepCleanEngine;
        _notificationService = notificationService;
        _soundService = soundService;
        _configService = configService;
        _historyService = historyService;
        _logger = logger;
        _statusCallback = statusCallback;

        ExecuteTaskCommand = new RelayCommand<string>(id =>
        {
            if (id != null) _ = ExecuteTaskAsync(id);
        });
        
        LoadTasks();
    }

    private void LoadTasks()
    {
        Tasks.Clear();
        
        Tasks.Add(new MaintenanceTask
        {
            Id = "DailyOptimization",
            Name = "⭐ Otimização Diária",
            Description = "Executa tarefas rápidas configuradas (arquivos temporários, limpeza de disco, etc.)",
            Command = "",
            RequiresRestart = false,
            Icon = "⭐",
            IsDeepClean = false
        });

        Tasks.Add(new MaintenanceTask
        {
            Id = "SystemRepair",
            Name = "🔧 Reparação do Sistema",
            Description = "Verifica integridade, repara imagem do Windows, verifica arquivos do sistema",
            Command = "",
            RequiresRestart = true,
            Icon = "🔧",
            IsDeepClean = false
        });

        Tasks.Add(new MaintenanceTask
        {
            Id = "LightClean",
            Name = "🧹 Limpeza Leve",
            Description = "Limpeza normal sem remover componentes antigos - remove arquivos temporários",
            Command = "",
            RequiresRestart = false,
            Icon = "🧹",
            IsDeepClean = false
        });

        Tasks.Add(new MaintenanceTask
        {
            Id = "DeepClean",
            Name = "🚀 Limpeza Profunda",
            Description = "Limpeza agressiva que remove componentes antigos definitivamente",
            Command = "",
            RequiresRestart = true,
            Icon = "🚀",
            IsDeepClean = true
        });

        Tasks.Add(new MaintenanceTask
        {
            Id = "RepairLightClean",
            Name = "🛠 Reparação + Limpeza Leve",
            Description = "Combina reparação do sistema com limpeza normal - ideal para manutenção preventiva",
            Command = "",
            RequiresRestart = true,
            Icon = "🛠",
            IsDeepClean = false
        });

        Tasks.Add(new MaintenanceTask
        {
            Id = "FullRepair",
            Name = "⚡ Reparação Completa",
            Description = "Combina reparação completa com limpeza agressiva - máxima otimização do sistema",
            Command = "",
            RequiresRestart = true,
            Icon = "⚡",
            IsDeepClean = true
        });
    }

    private void UpdateStatus(string text, int progress = 0, bool isRunning = true, bool isIndeterminate = true)
    {
        _statusCallback?.Invoke(text, progress, isRunning, isIndeterminate);
    }

    private async Task ExecuteTaskAsync(string? taskId)
    {
        if (string.IsNullOrEmpty(taskId)) return;
        if (_isExecuting)
        {
            _notificationService.ShowWarning("Aguarde", "Já existe uma tarefa em execução.");
            return;
        }

        try
        {
            _isExecuting = true;

            var task = Tasks.FirstOrDefault(t => t.Id == taskId);
            if (task == null) return;

            _logger.Info($"[MaintenanceViewModel] Usuário solicitou tarefa: {task.Name} ({task.Id})");

            var restartMsg = task.RequiresRestart ? "\n\n⚠️ RECOMENDA-SE REINICIAR O COMPUTADOR APÓS A CONCLUSÃO!" : "";
            var confirm = System.Windows.MessageBox.Show(
                $"Deseja executar: {task.Name}?\n\n{task.Description}{restartMsg}",
                "Confirmar Operação",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Question);

            if (confirm != System.Windows.MessageBoxResult.Yes) return;

            var progress = new Progress<string>(line => UpdateStatus(line));

            int exitCode = -1;

            switch (taskId)
            {
                case "DailyOptimization":
                    UpdateStatus("Executando limpeza de temporários...");
                    exitCode = await _maintenanceEngine.RunTaskAsync(new MaintenanceTask
                    {
                        Id = "TempFiles",
                        Name = "Limpeza de temporários",
                        Command = @"del /q /f /s ""%TEMP%\*"" && for /d %%x in (""%TEMP%\*"") do @rd /s /q ""%%x""",
                        RequiresRestart = false
                    }, progress);
                    UpdateStatus("Executando limpeza de disco...");
                    var exitCode2 = await _maintenanceEngine.RunTaskAsync(new MaintenanceTask
                    {
                        Id = "DiskCleanup",
                        Name = "Limpeza de Disco",
                        Command = "cleanmgr /sagerun:1",
                        RequiresRestart = false
                    }, progress);
                    if (exitCode == 0 && exitCode2 == 0)
                    {
                        UpdateStatus("Otimização Diária concluída com sucesso!", 100, false, false);
                        _soundService.PlaySuccess();
                        _notificationService.ShowSuccess("Otimização Diária", "Concluída com sucesso!");
                    }
                    else
                    {
                        UpdateStatus($"Otimização Diária concluída com avisos (código: {exitCode}, {exitCode2})", 100, false, false);
                        _soundService.PlayWarning();
                        _notificationService.ShowWarning("Otimização Diária", $"Concluída com avisos (código: {exitCode}, {exitCode2})");
                    }
                    break;

                case "SystemRepair":
                    UpdateStatus("Executando reparação do sistema...");
                    exitCode = await _repairEngine.RunSystemRepairAsync(progress);
                    if (exitCode == 0)
                    {
                        UpdateStatus("Reparação do Sistema concluída com sucesso!", 100, false, false);
                        _soundService.PlaySuccess();
                        _notificationService.ShowSuccess("Reparação do Sistema", "Concluída com sucesso!");
                    }
                    else
                    {
                        UpdateStatus($"Reparação do Sistema concluída com avisos (código: {exitCode})", 100, false, false);
                        _soundService.PlayWarning();
                        _notificationService.ShowWarning("Reparação do Sistema", $"Concluída com avisos (código: {exitCode})");
                    }
                    break;

                case "LightClean":
                    UpdateStatus("Executando limpeza leve...");
                    exitCode = await _maintenanceEngine.RunTaskAsync(new MaintenanceTask
                    {
                        Id = "LightClean",
                        Name = "Limpeza Leve",
                        Command = "cleanmgr /sageset:1 && cleanmgr /sagerun:1 && DISM /Online /Cleanup-Image /StartComponentCleanup",
                        RequiresRestart = false
                    }, progress);
                    if (exitCode == 0)
                    {
                        UpdateStatus("Limpeza Leve concluída com sucesso!", 100, false, false);
                        _soundService.PlaySuccess();
                        _notificationService.ShowSuccess("Limpeza Leve", "Concluída com sucesso!");
                    }
                    else
                    {
                        UpdateStatus($"Limpeza Leve concluída com avisos (código: {exitCode})", 100, false, false);
                        _soundService.PlayWarning();
                        _notificationService.ShowWarning("Limpeza Leve", $"Concluída com avisos (código: {exitCode})");
                    }
                    break;

                case "DeepClean":
                    UpdateStatus("Executando limpeza profunda...");
                    exitCode = await _deepCleanEngine.RunDeepCleanAsync(progress);
                    if (exitCode == 0)
                    {
                        UpdateStatus("Limpeza Profunda concluída com sucesso!", 100, false, false);
                        _soundService.PlaySuccess();
                        _notificationService.ShowSuccess("Limpeza Profunda", "Concluída com sucesso!");
                    }
                    else
                    {
                        UpdateStatus($"Limpeza Profunda concluída com avisos (código: {exitCode})", 100, false, false);
                        _soundService.PlayWarning();
                        _notificationService.ShowWarning("Limpeza Profunda", $"Concluída com avisos (código: {exitCode})");
                    }
                    break;

                case "RepairLightClean":
                    UpdateStatus("Executando reparação do sistema...");
                    exitCode = await _repairEngine.RunSystemRepairAsync(progress);
                    if (exitCode == 0)
                    {
                        UpdateStatus("Executando limpeza leve...");
                        await _maintenanceEngine.RunTaskAsync(new MaintenanceTask
                        {
                            Id = "LightClean",
                            Name = "Limpeza Leve",
                            Command = "cleanmgr /sageset:1 && cleanmgr /sagerun:1 && DISM /Online /Cleanup-Image /StartComponentCleanup",
                            RequiresRestart = false
                        }, progress);
                        UpdateStatus("Reparação + Limpeza Leve concluída com sucesso!", 100, false, false);
                        _soundService.PlaySuccess();
                        _notificationService.ShowSuccess("Reparação + Limpeza Leve", "Concluída com sucesso!");
                    }
                    else
                    {
                        UpdateStatus($"Reparação concluída com avisos (código: {exitCode})", 100, false, false);
                        _soundService.PlayWarning();
                        _notificationService.ShowWarning("Reparação + Limpeza Leve", $"Reparação concluída com avisos (código: {exitCode})");
                    }
                    break;

                case "FullRepair":
                    UpdateStatus("Executando reparação do sistema...");
                    exitCode = await _repairEngine.RunSystemRepairAsync(progress);
                    if (exitCode == 0)
                    {
                        UpdateStatus("Executando verificação de disco (CHKDSK)...");
                        exitCode = await _repairEngine.RunChkdskAsync(progress);
                        if (exitCode == 0)
                        {
                            UpdateStatus("Executando limpeza profunda...");
                            exitCode = await _deepCleanEngine.RunDeepCleanAsync(progress);
                        }
                    }
                    if (exitCode == 0)
                    {
                        UpdateStatus("Reparação Completa concluída com sucesso!", 100, false, false);
                        _soundService.PlaySuccess();
                        _notificationService.ShowSuccess("Reparação Completa", "Concluída com sucesso!");
                    }
                    else
                    {
                        UpdateStatus($"Reparação Completa concluída com avisos (código: {exitCode})", 100, false, false);
                        _soundService.PlayWarning();
                        _notificationService.ShowWarning("Reparação Completa", $"Concluída com avisos (código: {exitCode})");
                    }
                    break;
            }
        }
        catch (Exception ex)
        {
            UpdateStatus($"Erro: {ex.Message}", 0, false, false);
            _soundService.PlayError();
            _notificationService.ShowError("Erro", $"Ocorreu um erro durante a execução:\n{ex.Message}");
            _logger.Error($"[MaintenanceViewModel] Exceção na tarefa '{taskId}'", ex);
        }
        finally
        {
            _isExecuting = false;
        }
    }
}