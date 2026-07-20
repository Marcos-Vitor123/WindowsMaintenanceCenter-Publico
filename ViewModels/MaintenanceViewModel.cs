using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using WindowsMaintenanceCenter.Models;
using WindowsMaintenanceCenter.Services;
using WindowsMaintenanceCenter.Views;

namespace WindowsMaintenanceCenter.ViewModels;

public class MaintenanceViewModel : ViewModelBase
{
    private readonly MaintenanceEngine _maintenanceEngine;
    private readonly SystemRepairEngine _repairEngine;
    private readonly DeepCleanEngine _deepCleanEngine;
    private readonly DiskCleanupService _diskCleanupService;
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
        DiskCleanupService diskCleanupService,
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
        _diskCleanupService = diskCleanupService;
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

            var progressWindow = new TaskProgressWindow
            {
                Owner = Application.Current.MainWindow
            };

            var (totalSteps, taskIcon, taskTitle, taskSubtitle) = GetTaskMeta(taskId);
            progressWindow.SetTaskInfo(taskIcon, taskTitle, taskSubtitle);
            progressWindow.ProgressBar.Maximum = 100;
            progressWindow.ProgressBar.IsIndeterminate = false;
            progressWindow.UpdateProgress(0, "Iniciando...");

            var currentStep = 0;
            var progress = new Progress<string>(line =>
            {
                progressWindow.AppendLog(line);
            });

            var cts = new CancellationTokenSource();
            progressWindow.CancelRequested += () => cts.Cancel();

            progressWindow.Show();

            int exitCode = -1;

            switch (taskId)
            {
                case "DailyOptimization":
                    progressWindow.UpdateProgress(0, "Limpando arquivos temporários...");
                    currentStep++;
                    exitCode = await _maintenanceEngine.RunTaskAsync(new MaintenanceTask
                    {
                        Id = "TempFiles",
                        Name = "TempFiles - Limpeza de temporários",
                        Command = @"del /q /f /s ""%TEMP%\*"" & for /d %%x in (""%TEMP%\*"") do @rd /s /q ""%%x""",
                        RequiresRestart = false
                    }, progress);

                    progressWindow.UpdateProgress((double)currentStep / totalSteps * 100, "Limpando discos...");
                    currentStep++;
                    var config = _configService.GetConfig();
                    var drives = config.SelectedDrives?.Count > 0
                        ? config.SelectedDrives
                        : new List<string> { "C:" };
                    var exitCode2 = await _diskCleanupService.RunCleanupForDrivesAsync(drives, progress);

                    _historyService.AddEntry(new HistoryEntry
                    {
                        Date = DateTime.Now,
                        FunctionName = "Otimização Diária",
                        Command = "DailyOptimization",
                        Success = exitCode <= 1 && exitCode2 <= 1,
                        ExitCode = exitCode,
                        Duration = TimeSpan.Zero
                    });

                    if (exitCode <= 1 && exitCode2 <= 1)
                    {
                        progressWindow.MarkCompleted(true, "Otimização Diária concluída com sucesso!");
                        _soundService.PlaySuccess();
                        _notificationService.ShowSuccess("Otimização Diária", "Concluída com sucesso!");
                    }
                    else
                    {
                        progressWindow.MarkCompleted(false, $"Otimização Diária concluída com avisos (código: {exitCode}, {exitCode2})");
                        _soundService.PlayWarning();
                        _notificationService.ShowWarning("Otimização Diária", $"Concluída com avisos (código: {exitCode}, {exitCode2})");
                    }
                    break;

                case "SystemRepair":
                    progressWindow.UpdateProgress(0, "Verificando integridade do sistema...");
                    exitCode = await _repairEngine.RunSystemRepairAsync(progress);
                    currentStep = totalSteps;

                    _historyService.AddEntry(new HistoryEntry
                    {
                        Date = DateTime.Now,
                        FunctionName = "SystemRepair - Reparação do Sistema",
                        Command = "SystemRepair",
                        Success = exitCode == 0,
                        ExitCode = exitCode,
                        Duration = TimeSpan.Zero
                    });
                    if (exitCode == 0)
                    {
                        progressWindow.MarkCompleted(true, "Reparação do Sistema concluída com sucesso!");
                        _soundService.PlaySuccess();
                        _notificationService.ShowSuccess("Reparação do Sistema", "Concluída com sucesso!");
                    }
                    else
                    {
                        progressWindow.MarkCompleted(false, $"Reparação do Sistema concluída com avisos (código: {exitCode})");
                        _soundService.PlayWarning();
                        _notificationService.ShowWarning("Reparação do Sistema", $"Concluída com avisos (código: {exitCode})");
                    }
                    break;

                case "LightClean":
                    progressWindow.UpdateProgress(0, "Limpando discos...");
                    {
                        var lcConfig = _configService.GetConfig();
                        var lcDrives = lcConfig.SelectedDrives?.Count > 0
                            ? lcConfig.SelectedDrives
                            : new List<string> { "C:" };
                        exitCode = await _diskCleanupService.RunCleanupForDrivesAsync(lcDrives, progress);
                    }
                    progressWindow.UpdateProgress(50, "Executando limpeza de componentes...");
                    {
                        var dismExit = await _maintenanceEngine.RunTaskAsync(new MaintenanceTask
                        {
                            Id = "LightClean",
                            Name = "Limpeza Leve - DISM",
                            Command = "DISM /Online /Cleanup-Image /StartComponentCleanup",
                            RequiresRestart = false
                        }, progress);
                        if (dismExit != 0) exitCode = dismExit;
                    }
                    currentStep = totalSteps;
                    if (exitCode == 0)
                    {
                        progressWindow.MarkCompleted(true, "Limpeza Leve concluída com sucesso!");
                        _soundService.PlaySuccess();
                        _notificationService.ShowSuccess("Limpeza Leve", "Concluída com sucesso!");
                    }
                    else
                    {
                        progressWindow.MarkCompleted(false, $"Limpeza Leve concluída com avisos (código: {exitCode})");
                        _soundService.PlayWarning();
                        _notificationService.ShowWarning("Limpeza Leve", $"Concluída com avisos (código: {exitCode})");
                    }
                    break;

                case "DeepClean":
                    progressWindow.UpdateProgress(0, "Executando limpeza profunda...");
                    exitCode = await _deepCleanEngine.RunDeepCleanAsync(progress);
                    currentStep = totalSteps;
                    if (exitCode == 0)
                    {
                        progressWindow.MarkCompleted(true, "Limpeza Profunda concluída com sucesso!");
                        _soundService.PlaySuccess();
                        _notificationService.ShowSuccess("Limpeza Profunda", "Concluída com sucesso!");
                    }
                    else
                    {
                        progressWindow.MarkCompleted(false, $"Limpeza Profunda concluída com avisos (código: {exitCode})");
                        _soundService.PlayWarning();
                        _notificationService.ShowWarning("Limpeza Profunda", $"Concluída com avisos (código: {exitCode})");
                    }
                    break;

                case "RepairLightClean":
                    progressWindow.UpdateProgress(0, "Executando reparação do sistema...");
                    exitCode = await _repairEngine.RunSystemRepairAsync(progress);
                    currentStep = 4;
                    progressWindow.UpdateProgress((double)currentStep / totalSteps * 100, "Executando limpeza leve...");
                    if (exitCode == 0)
                    {
                        progressWindow.UpdateProgress((double)currentStep / totalSteps * 100, "Limpando discos...");
                        {
                            var rlcConfig = _configService.GetConfig();
                            var rlcDrives = rlcConfig.SelectedDrives?.Count > 0
                                ? rlcConfig.SelectedDrives
                                : new List<string> { "C:" };
                            await _diskCleanupService.RunCleanupForDrivesAsync(rlcDrives, progress);
                        }
                        progressWindow.UpdateProgress((double)(currentStep + 1) / totalSteps * 100, "Executando limpeza de componentes...");
                        await _maintenanceEngine.RunTaskAsync(new MaintenanceTask
                        {
                            Id = "LightClean",
                            Name = "Limpeza Leve - DISM",
                            Command = "DISM /Online /Cleanup-Image /StartComponentCleanup",
                            RequiresRestart = false
                        }, progress);
                        currentStep = totalSteps;
                        progressWindow.MarkCompleted(true, "Reparação + Limpeza Leve concluída com sucesso!");
                        _soundService.PlaySuccess();
                        _notificationService.ShowSuccess("Reparação + Limpeza Leve", "Concluída com sucesso!");
                    }
                    else
                    {
                        progressWindow.MarkCompleted(false, $"Reparação concluída com avisos (código: {exitCode})");
                        _soundService.PlayWarning();
                        _notificationService.ShowWarning("Reparação + Limpeza Leve", $"Reparação concluída com avisos (código: {exitCode})");
                    }
                    break;

                case "FullRepair":
                    progressWindow.UpdateProgress(0, "Executando reparação do sistema...");
                    exitCode = await _repairEngine.RunSystemRepairAsync(progress);
                    currentStep = 4;
                    if (exitCode == 0)
                    {
                        progressWindow.UpdateProgress((double)currentStep / totalSteps * 100, "Verificando disco (CHKDSK)...");
                        exitCode = await _repairEngine.RunChkdskAsync(progress);
                        currentStep = 5;
                        if (exitCode == 0)
                        {
                            progressWindow.UpdateProgress((double)currentStep / totalSteps * 100, "Executando limpeza profunda...");
                            exitCode = await _deepCleanEngine.RunDeepCleanAsync(progress);
                            currentStep = totalSteps;
                        }
                    }
                    if (exitCode == 0)
                    {
                        progressWindow.MarkCompleted(true, "Reparação Completa concluída com sucesso!");
                        _soundService.PlaySuccess();
                        _notificationService.ShowSuccess("Reparação Completa", "Concluída com sucesso!");
                    }
                    else
                    {
                        progressWindow.MarkCompleted(false, $"Reparação Completa concluída com avisos (código: {exitCode})");
                        _soundService.PlayWarning();
                        _notificationService.ShowWarning("Reparação Completa", $"Concluída com avisos (código: {exitCode})");
                    }
                    break;
            }
        }
        catch (Exception ex)
        {
            _soundService.PlayError();
            _notificationService.ShowError("Erro", $"Ocorreu um erro durante a execução:\n{ex.Message}");
            _logger.Error($"[MaintenanceViewModel] Exceção na tarefa '{taskId}'", ex);
        }
        finally
        {
            _isExecuting = false;
        }
    }

    private static (int totalSteps, string icon, string title, string subtitle) GetTaskMeta(string taskId)
    {
        return taskId switch
        {
            "DailyOptimization" => (2, "⭐", "Otimização Diária", "Executando tarefas rápidas de manutenção..."),
            "SystemRepair" => (4, "🔧", "Reparação do Sistema", "Verificando e reparando arquivos do Windows..."),
            "LightClean" => (1, "🧹", "Limpeza Leve", "Removendo arquivos temporários..."),
            "DeepClean" => (3, "🚀", "Limpeza Profunda", "Removendo componentes antigos e temporários..."),
            "RepairLightClean" => (5, "🛠", "Reparação + Limpeza Leve", "Reparação do sistema e limpeza combinadas..."),
            "FullRepair" => (8, "⚡", "Reparação Completa", "Reparação, verificação de disco e limpeza profunda..."),
            _ => (1, "⚙️", "Executando...", "Aguarde...")
        };
    }
}
