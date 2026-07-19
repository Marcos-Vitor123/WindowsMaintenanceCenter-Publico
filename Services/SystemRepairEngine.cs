using System.Diagnostics;
using System.IO;
using WindowsMaintenanceCenter.Models;

namespace WindowsMaintenanceCenter.Services;

public class SystemRepairEngine
{
    private readonly HistoryService _historyService;
    private readonly SoundService _soundService;

    public event Action<string, int>? TaskProgress;
    public event Action<string>? TaskCompleted;
    public event Action<string>? TaskError;

    public SystemRepairEngine(HistoryService historyService, SoundService soundService)
    {
        _historyService = historyService;
        _soundService = soundService;
    }

    public async Task<int> RunSystemRepairAsync(IProgress<string>? progress = null)
    {
        var startTime = DateTime.Now;
        var entry = new HistoryEntry
        {
            Date = startTime,
            FunctionName = "Reparação do Sistema",
            Command = "DISM /CheckHealth /ScanHealth /RestoreHealth && sfc /scannow"
        };

        var commands = new[]
        {
            ("DISM CheckHealth", "DISM /Online /Cleanup-Image /CheckHealth"),
            ("DISM ScanHealth", "DISM /Online /Cleanup-Image /ScanHealth"),
            ("DISM RestoreHealth", "DISM /Online /Cleanup-Image /RestoreHealth"),
            ("SFC ScanNow", "sfc /scannow")
        };

        int lastExitCode = 0;

        try
        {
            foreach (var (name, cmd) in commands)
            {
                progress?.Report($"Executando: {name}");
                TaskProgress?.Invoke(name, 0);

                var exitCode = await ExecuteCommandAsync(cmd, progress);
                lastExitCode = exitCode;

                if (exitCode != 0)
                {
                    progress?.Report($"⚠️ {name} retornou código {exitCode}");
                }
                else
                {
                    progress?.Report($"✅ {name} concluído");
                }
            }

            entry.Success = lastExitCode == 0;
            entry.ExitCode = lastExitCode;
            entry.Duration = DateTime.Now - startTime;
            _historyService.AddEntry(entry);
            TaskCompleted?.Invoke("Reparação do Sistema concluída");

            if (lastExitCode == 0)
                _soundService.PlayComplete();
            else
                _soundService.PlayWarning();

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
            throw;
        }
    }

    public async Task<int> RunChkdskAsync(IProgress<string>? progress = null)
    {
        var startTime = DateTime.Now;
        var entry = new HistoryEntry
        {
            Date = startTime,
            FunctionName = "Verificação de Disco (CHKDSK)",
            Command = "chkdsk C: /f /r"
        };

        try
        {
            progress?.Report("Agendando verificação de disco para próxima reinicialização...");
            TaskProgress?.Invoke("CHKDSK", 0);

            var exitCode = await ExecuteCommandAsync("echo Y|chkdsk C: /f /r", progress);

            entry.Success = exitCode == 0;
            entry.ExitCode = exitCode;
            entry.Duration = DateTime.Now - startTime;
            _historyService.AddEntry(entry);
            TaskCompleted?.Invoke("CHKDSK agendado");

            if (exitCode == 0)
                _soundService.PlayComplete();
            else
                _soundService.PlayWarning();

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
            StandardOutputEncoding = System.Text.Encoding.GetEncoding(850),
            StandardErrorEncoding = System.Text.Encoding.GetEncoding(850)
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