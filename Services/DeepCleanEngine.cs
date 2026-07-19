using System.Diagnostics;
using System.IO;
using WindowsMaintenanceCenter.Models;

namespace WindowsMaintenanceCenter.Services;

public class DeepCleanEngine
{
    private readonly HistoryService _historyService;
    private readonly SoundService _soundService;
    private readonly LoggingService _logger;

    public event Action<string, int>? TaskProgress;
    public event Action<string>? TaskCompleted;
    public event Action<string>? TaskError;

    public DeepCleanEngine(HistoryService historyService, SoundService soundService, LoggingService logger)
    {
        _historyService = historyService;
        _soundService = soundService;
        _logger = logger;
    }

    public async Task<int> RunDeepCleanAsync(IProgress<string>? progress = null)
    {
        var startTime = DateTime.Now;
        var entry = new HistoryEntry
        {
            Date = startTime,
            FunctionName = "Limpeza Profunda + Imagens Antigas",
            Command = "cleanmgr /sagerun:1 && DISM /StartComponentCleanup /ResetBase"
        };

        _logger.Info("[DeepCleanEngine] Iniciando limpeza profunda");

        var commands = new[]
        {
            ("Configurar Limpeza", "cleanmgr /sageset:1"),
            ("Executar Limpeza", "cleanmgr /sagerun:1"),
            ("Limpeza Componentes (ResetBase)", "DISM /Online /Cleanup-Image /StartComponentCleanup /ResetBase")
        };

        int lastExitCode = 0;

        try
        {
            foreach (var (name, cmd) in commands)
            {
                progress?.Report($"Executando: {name}");
                TaskProgress?.Invoke(name, 0);
                _logger.Info($"[DeepCleanEngine] Executando: {name} | Comando: {cmd}");

                var exitCode = await ExecuteCommandAsync(cmd, progress);
                lastExitCode = exitCode;

                if (exitCode != 0)
                {
                    progress?.Report($"⚠️ {name} retornou código {exitCode}");
                    _logger.Warn($"[DeepCleanEngine] {name} retornou código {exitCode}");
                }
                else
                {
                    progress?.Report($"✅ {name} concluído");
                    _logger.Info($"[DeepCleanEngine] {name} concluído com sucesso");
                }
            }

            entry.Success = lastExitCode == 0;
            entry.ExitCode = lastExitCode;
            entry.Duration = DateTime.Now - startTime;
            entry.SpaceFreedBytes = 2L * 1024 * 1024 * 1024; // ~2GB estimate
            _historyService.AddEntry(entry);
            TaskCompleted?.Invoke("Limpeza Profunda concluída");

            if (lastExitCode == 0)
                _soundService.PlayComplete();
            else
                _soundService.PlayWarning();

            _logger.Info($"[DeepCleanEngine] Limpeza profunda concluída (exit={lastExitCode}, duração={entry.Duration.TotalSeconds:F1}s)");
            return lastExitCode;
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