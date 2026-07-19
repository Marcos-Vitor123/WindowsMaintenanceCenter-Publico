using System.Collections.Generic;
using WindowsMaintenanceCenter.Models;
using WindowsMaintenanceCenter.Services;

namespace WindowsMaintenanceCenter.Services;

public class AutomationService
{
    private readonly ConfigService _configService;
    private readonly HistoryService _historyService;
    private readonly MaintenanceEngine _maintenanceEngine;
    private readonly SystemRepairEngine _repairEngine;
    private readonly DeepCleanEngine _deepCleanEngine;
    private Timer? _checkTimer;

    public AutomationService(
        ConfigService configService,
        HistoryService historyService,
        MaintenanceEngine maintenanceEngine,
        SystemRepairEngine repairEngine,
        DeepCleanEngine deepCleanEngine)
    {
        _configService = configService;
        _historyService = historyService;
        _maintenanceEngine = maintenanceEngine;
        _repairEngine = repairEngine;
        _deepCleanEngine = deepCleanEngine;
    }

    public void Start()
    {
        _checkTimer = new Timer(CheckPendingTasks, null, TimeSpan.FromMinutes(1), TimeSpan.FromHours(1));
    }

    public void Stop()
    {
        _checkTimer?.Dispose();
        _checkTimer = null;
    }

    private async void CheckPendingTasks(object? state)
    {
        var config = _configService.GetConfig();
        if (!config.AutoMode) return;

        var history = _historyService.GetHistory();
        var now = DateTime.Now;

        foreach (var schedule in config.TaskSchedules)
        {
            if (!schedule.Value.Enabled) continue;

            var lastRun = history.FirstOrDefault(h => h.FunctionName.Contains(schedule.Key, StringComparison.OrdinalIgnoreCase))?.Date;
            if (lastRun == null || (now - lastRun.Value).TotalDays >= schedule.Value.IntervalDays)
            {
                await RunScheduledTaskAsync(schedule.Key);
            }
        }
    }

    private async Task RunScheduledTaskAsync(string taskKey)
    {
        try
        {
            switch (taskKey)
            {
                case "TempFiles":
                    await _maintenanceEngine.RunTaskAsync(new MaintenanceTask
                    {
                        Id = "TempFiles",
                        Name = "Limpeza automática de temporários",
                        Command = @"del /q /f /s ""%TEMP%\*"" && for /d %%x in (""%TEMP%\*"") do @rd /s /q ""%%x""",
                        RequiresRestart = false
                    });
                    break;
                case "DiskCleanup":
                    await _maintenanceEngine.RunTaskAsync(new MaintenanceTask
                    {
                        Id = "DiskCleanup",
                        Name = "Limpeza de Disco automática",
                        Command = "cleanmgr /sagerun:1",
                        RequiresRestart = false
                    });
                    break;
                case "SystemRepair":
                    await _repairEngine.RunSystemRepairAsync();
                    break;
            }
        }
        catch { }
    }

    public async Task CheckBeforeShutdownAsync()
    {
        var config = _configService.GetConfig();
        var history = _historyService.GetHistory();
        var now = DateTime.Now;

        var dailyOpt = history.FirstOrDefault(h => h.FunctionName.Contains("Otimização Diária"));
        if (dailyOpt == null || (now - dailyOpt.Date).TotalDays >= 1)
        {
            // Would show notification to user - handled by UI
        }
    }
}