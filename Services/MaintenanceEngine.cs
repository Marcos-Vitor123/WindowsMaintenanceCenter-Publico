using System.Diagnostics;
using System.IO;
using System.Text;
using WindowsMaintenanceCenter.Models;

namespace WindowsMaintenanceCenter.Services;

public class MaintenanceEngine
{
    private readonly HistoryService _historyService;
    private readonly SoundService _soundService;
    private readonly LoggingService _logger;

    public event Action<string, int>? TaskProgress; // status, progress
    public event Action<MaintenanceTask, int>? TaskCompleted; // task, exitCode
    public event Action<string>? TaskError;

    public MaintenanceEngine(HistoryService historyService, SoundService soundService, LoggingService logger)
    {
        _historyService = historyService;
        _soundService = soundService;
        _logger = logger;
    }

    public async Task<int> RunTaskAsync(MaintenanceTask task, IProgress<string>? progress = null)
    {
        var startTime = DateTime.Now;
        var entry = new HistoryEntry
        {
            Date = startTime,
            FunctionName = task.Name,
            Command = task.Command
        };

        _logger.Info($"[MaintenanceEngine] Iniciando tarefa: {task.Name} | Comando: {task.Command}");

        try
        {
            progress?.Report($"Iniciando: {task.Name}");
            TaskProgress?.Invoke($"Executando: {task.Name}", 0);

            var exitCode = await ExecuteCommandAsync(task.Command, progress);

            entry.Success = exitCode == 0 || exitCode == 1;
            entry.ExitCode = exitCode;
            entry.Duration = DateTime.Now - startTime;
            entry.SpaceFreedBytes = EstimateSpaceFreed(task.Id, exitCode);

            _historyService.AddEntry(entry);
            TaskCompleted?.Invoke(task, exitCode);

            if (exitCode == 0 || exitCode == 1)
            {
                _soundService.PlayComplete();
                progress?.Report($"✅ {task.Name} concluído com sucesso!");
                _logger.Info($"[MaintenanceEngine] Tarefa '{task.Name}' concluída com sucesso (exit={exitCode}, duração={entry.Duration.TotalSeconds:F1}s)");
            }
            else
            {
                _soundService.PlayWarning();
                progress?.Report($"⚠️ {task.Name} concluído com avisos (código: {exitCode})");
                _logger.Warn($"[MaintenanceEngine] Tarefa '{task.Name}' concluída com avisos (exit={exitCode}, duração={entry.Duration.TotalSeconds:F1}s)");
            }

            return exitCode;
        }
        catch (Exception ex)
        {
            entry.Success = false;
            entry.ErrorMessage = ex.Message;
            entry.Duration = DateTime.Now - startTime;
            _historyService.AddEntry(entry);
            TaskError?.Invoke(ex.Message);
            _soundService.PlayError();
            _logger.Error($"[MaintenanceEngine] Exceção na tarefa '{task.Name}'", ex);
            throw;
        }
    }

    private async Task<int> ExecuteCommandAsync(string command, IProgress<string>? progress)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = $"/c {command}",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = System.Text.Encoding.UTF8,
            StandardErrorEncoding = System.Text.Encoding.UTF8
        };

        using var process = new Process { StartInfo = startInfo };

        var outputBuilder = new System.Text.StringBuilder();
        var errorBuilder = new System.Text.StringBuilder();
        var outputLock = new object();

        process.OutputDataReceived += (s, e) =>
        {
            if (e.Data != null)
            {
                lock (outputLock)
                {
                    outputBuilder.AppendLine(e.Data);
                }
                progress?.Report(e.Data);
            }
        };

        process.ErrorDataReceived += (s, e) =>
        {
            if (e.Data != null)
            {
                lock (outputLock)
                {
                    errorBuilder.AppendLine(e.Data);
                }
            }
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        await process.WaitForExitAsync();

        return process.ExitCode;
    }

    private long EstimateSpaceFreed(string taskId, int exitCode)
    {
        if (exitCode != 0 && exitCode != 1) return 0;
        return taskId switch
        {
            "TempFiles" => 500 * 1024 * 1024,
            "DiskCleanup" => 1024 * 1024 * 1024,
            "Prefetch" => 200 * 1024 * 1024,
            _ => 0
        };
    }
}