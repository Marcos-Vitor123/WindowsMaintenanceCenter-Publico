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
        HistoryService historyService)
    {
        _maintenanceEngine = maintenanceEngine;
        _repairEngine = repairEngine;
        _deepCleanEngine = deepCleanEngine;
        _notificationService = notificationService;
        _soundService = soundService;
        _configService = configService;
        _historyService = historyService;

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

            var restartMsg = task.RequiresRestart ? "\n\n⚠️ RECOMENDA-SE REINICIAR O COMPUTADOR APÓS A CONCLUSÃO!" : "";
            var confirm = System.Windows.MessageBox.Show(
                $"Deseja executar: {task.Name}?\n\n{task.Description}{restartMsg}",
                "Confirmar Operação",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Question);

            if (confirm != System.Windows.MessageBoxResult.Yes) return;

            int exitCode = -1;

            switch (taskId)
            {
                case "DailyOptimization":
                    exitCode = await _maintenanceEngine.RunTaskAsync(new MaintenanceTask
                    {
                        Id = "TempFiles",
                        Name = "Limpeza de temporários",
                        Command = @"del /q /f /s ""%TEMP%\*"" && for /d %%x in (""%TEMP%\*"") do @rd /s /q ""%%x""",
                        RequiresRestart = false
                    });
                    var exitCode2 = await _maintenanceEngine.RunTaskAsync(new MaintenanceTask
                    {
                        Id = "DiskCleanup",
                        Name = "Limpeza de Disco",
                        Command = "cleanmgr /sagerun:1",
                        RequiresRestart = false
                    });
                    if (exitCode == 0 && exitCode2 == 0)
                        _notificationService.ShowSuccess("Otimização Diária", "Concluída com sucesso!");
                    else
                        _notificationService.ShowWarning("Otimização Diária", $"Concluída com avisos (código: {exitCode}, {exitCode2})");
                    break;

                case "SystemRepair":
                    exitCode = await _repairEngine.RunSystemRepairAsync();
                    if (exitCode == 0)
                        _notificationService.ShowSuccess("Reparação do Sistema", "Concluída com sucesso!");
                    else
                        _notificationService.ShowWarning("Reparação do Sistema", $"Concluída com avisos (código: {exitCode})");
                    break;

                case "LightClean":
                    exitCode = await _maintenanceEngine.RunTaskAsync(new MaintenanceTask
                    {
                        Id = "LightClean",
                        Name = "Limpeza Leve",
                        Command = "cleanmgr /sageset:1 && cleanmgr /sagerun:1 && DISM /Online /Cleanup-Image /StartComponentCleanup",
                        RequiresRestart = false
                    });
                    if (exitCode == 0)
                        _notificationService.ShowSuccess("Limpeza Leve", "Concluída com sucesso!");
                    else
                        _notificationService.ShowWarning("Limpeza Leve", $"Concluída com avisos (código: {exitCode})");
                    break;

                case "DeepClean":
                    exitCode = await _deepCleanEngine.RunDeepCleanAsync();
                    if (exitCode == 0)
                        _notificationService.ShowSuccess("Limpeza Profunda", "Concluída com sucesso!");
                    else
                        _notificationService.ShowWarning("Limpeza Profunda", $"Concluída com avisos (código: {exitCode})");
                    break;

                case "RepairLightClean":
                    exitCode = await _repairEngine.RunSystemRepairAsync();
                    if (exitCode == 0)
                    {
                        await _maintenanceEngine.RunTaskAsync(new MaintenanceTask
                        {
                            Id = "LightClean",
                            Name = "Limpeza Leve",
                            Command = "cleanmgr /sageset:1 && cleanmgr /sagerun:1 && DISM /Online /Cleanup-Image /StartComponentCleanup",
                            RequiresRestart = false
                        });
                        _notificationService.ShowSuccess("Reparação + Limpeza Leve", "Concluída com sucesso!");
                    }
                    else
                    {
                        _notificationService.ShowWarning("Reparação + Limpeza Leve", $"Reparação concluída com avisos (código: {exitCode})");
                    }
                    break;

                case "FullRepair":
                    exitCode = await _repairEngine.RunSystemRepairAsync();
                    if (exitCode == 0)
                    {
                        exitCode = await _repairEngine.RunChkdskAsync();
                        if (exitCode == 0)
                        {
                            exitCode = await _deepCleanEngine.RunDeepCleanAsync();
                        }
                    }
                    if (exitCode == 0)
                        _notificationService.ShowSuccess("Reparação Completa", "Concluída com sucesso!");
                    else
                        _notificationService.ShowWarning("Reparação Completa", $"Concluída com avisos (código: {exitCode})");
                    break;
            }
        }
        catch (Exception ex)
        {
            _notificationService.ShowError("Erro", $"Ocorreu um erro durante a execução:\n{ex.Message}");
        }
        finally
        {
            _isExecuting = false;
        }
    }
}