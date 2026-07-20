using System.Diagnostics;
using WindowsMaintenanceCenter.Models;

namespace WindowsMaintenanceCenter.Services;

public class DeepCleanEngine
{
    private readonly HistoryService _historyService;
    private readonly SoundService _soundService;
    private readonly LoggingService _logger;
    private readonly DiskCleanupService _diskCleanupService;
    private readonly ConfigService _configService;

    public event Action<string, int>? TaskProgress;
    public event Action<string>? TaskCompleted;
    public event Action<string>? TaskError;

    public DeepCleanEngine(HistoryService historyService, SoundService soundService, LoggingService logger,
        DiskCleanupService diskCleanupService, ConfigService configService)
    {
        _historyService = historyService;
        _soundService = soundService;
        _logger = logger;
        _diskCleanupService = diskCleanupService;
        _configService = configService;
    }

    public async Task<int> RunDeepCleanAsync(IProgress<string>? progress = null)
    {
        var startTime = DateTime.Now;
        var entry = new HistoryEntry
        {
            Date = startTime,
            FunctionName = "Limpeza Profunda + Imagens Antigas",
            Command = "Cleanup + DISM /StartComponentCleanup /ResetBase"
        };

        _logger.Info("[DeepCleanEngine] Iniciando limpeza profunda");

        var config = _configService.GetConfig();
        var selectedDrives = config.SelectedDrives?.Count > 0
            ? config.SelectedDrives
            : new List<string> { "C:" };

        int lastExitCode = 0;

        try
        {
            progress?.Report("Limpando arquivos temporários...");
            lastExitCode = await _diskCleanupService.RunCleanupForDrivesAsync(selectedDrives, progress);

            progress?.Report("Executando limpeza de componentes (ResetBase)...");
            _logger.Info("[DeepCleanEngine] Executando DISM ResetBase");
            var dismExit = await ExecuteCommandAsync(
                "DISM /Online /Cleanup-Image /StartComponentCleanup /ResetBase", progress);

            if (dismExit != 0)
            {
                progress?.Report($"Aviso: DISM retornou código {dismExit}");
                _logger.Warn($"[DeepCleanEngine] DISM retornou código {dismExit}");
            }
            else
            {
                progress?.Report("Limpeza de componentes concluída");
                _logger.Info("[DeepCleanEngine] DISM ResetBase concluído");
            }

            entry.Success = lastExitCode == 0 && dismExit == 0;
            entry.ExitCode = dismExit != 0 ? dismExit : lastExitCode;
            entry.Duration = DateTime.Now - startTime;
            entry.SpaceFreedBytes = 2L * 1024 * 1024 * 1024;
            _historyService.AddEntry(entry);
            TaskCompleted?.Invoke("Limpeza Profunda concluída");

            if (entry.Success)
                _soundService.PlayComplete();
            else
                _soundService.PlayWarning();

            _logger.Info($"[DeepCleanEngine] Limpeza profunda concluída (exit={entry.ExitCode}, duração={entry.Duration.TotalSeconds:F1}s)");
            return entry.ExitCode;
        }
        catch (Exception ex)
        {
            entry.Success = false;
            entry.ErrorMessage = ex.Message;
            entry.Duration = DateTime.Now - startTime;
            _historyService.AddEntry(entry);
            TaskError?.Invoke(ex.Message);
            _soundService.PlayError();
            _logger.Error("[DeepCleanEngine] Exceção na limpeza profunda", ex);
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

        process.OutputDataReceived += (s, e) =>
        {
            if (e.Data != null)
                progress?.Report(e.Data);
        };

        process.ErrorDataReceived += (s, e) =>
        {
            if (e.Data != null)
                progress?.Report(e.Data);
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        await process.WaitForExitAsync();

        return process.ExitCode;
    }
}
