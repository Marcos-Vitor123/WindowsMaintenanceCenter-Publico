using System.Diagnostics;
using System.IO;

namespace WindowsMaintenanceCenter.Services;

public class DiskCleanupService
{
    private readonly LoggingService _logger;

    public DiskCleanupService(LoggingService logger)
    {
        _logger = logger;
    }

    public List<string> GetAvailableDrives()
    {
        return DriveInfo.GetDrives()
            .Where(d => d.DriveType == DriveType.Fixed
                     && d.IsReady
                     && d.Name.Length == 3)
            .Select(d => d.Name.TrimEnd('\\').ToUpper())
            .OrderBy(d => d)
            .ToList();
    }

    public async Task<int> RunCleanupForDrivesAsync(List<string> selectedDrives, IProgress<string>? progress = null)
    {
        if (selectedDrives.Count == 0)
            selectedDrives = new List<string> { "C:" };

        int lastExit = 0;
        foreach (var drive in selectedDrives)
        {
            progress?.Report($"Limpando disco {drive}...");
            _logger.Info($"[DiskCleanupService] Iniciando limpeza no disco {drive}");

            lastExit = await RunDriveCleanupAsync(drive, progress);
            _logger.Info($"[DiskCleanupService] Limpeza {drive} concluída (exit: {lastExit})");
        }

        return lastExit;
    }

    private async Task<int> RunDriveCleanupAsync(string drive, IProgress<string>? progress)
    {
        var driveLetter = drive.TrimEnd('\\');
        var exitCode = 0;

        var tempPaths = new[]
        {
            $@"{driveLetter}\Windows\Temp",
            $@"{driveLetter}\Windows\Prefetch",
            $@"%TEMP%",
            $@"%LOCALAPPDATA%\Temp",
            $@"%LOCALAPPDATA%\Microsoft\Windows\INetCache",
            $@"%LOCALAPPDATA%\Microsoft\Windows\Explorer",
            $@"%LOCALAPPDATA%\CrashDumps",
            $@"%LOCALAPPDATA%\D3DSCache",
            $@"%LOCALAPPDATA%\NuGet",
            $@"%LOCALAPPDATA%\pip\cache",
            $@"%USERPROFILE%\AppData\Local\Microsoft\Windows\WER"
        };

        var cleanupCommands = new List<string>();

        foreach (var path in tempPaths)
        {
            cleanupCommands.Add($@"del /q /f /s ""{path}\*"" 2>nul");
            cleanupCommands.Add($@"for /d %%x in (""{path}\*"") do @rd /s /q ""%%x"" 2>nul");
        }

        cleanupCommands.Add($@"del /q /f /s ""{driveLetter}\Windows\SoftwareDistribution\Download\*"" 2>nul");
        cleanupCommands.Add($@"for /d %%x in (""{driveLetter}\Windows\SoftwareDistribution\Download\*"") do @rd /s /q ""%%x"" 2>nul");

        cleanupCommands.Add($@"del /q /f /s ""{driveLetter}\Windows\Installer\*.tmp"" 2>nul");

        cleanupCommands.Add($@"del /q /f /s ""{driveLetter}\Windows\Logs\CBS\*.log"" 2>nul");
        cleanupCommands.Add($@"del /q /f /s ""{driveLetter}\Windows\Logs\DISM\*.log"" 2>nul");
        cleanupCommands.Add($@"del /q /f /s ""{driveLetter}\Windows\Logs\WindowsUpdate\*.log"" 2>nul");

        cleanupCommands.Add($@"del /q /f /s ""{driveLetter}\Windows\Minidump\*.dmp"" 2>nul");
        cleanupCommands.Add($@"del /q /f /s ""{driveLetter}\Windows\MEMORY.DMP"" 2>nul");

        var batchContent = string.Join(Environment.NewLine, cleanupCommands);
        var tempFile = Path.Combine(Path.GetTempPath(), $"wmc_cleanup_{driveLetter[0]}.bat");

        try
        {
            File.WriteAllText(tempFile, batchContent);

            var startInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c \"{tempFile}\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = System.Text.Encoding.UTF8,
                StandardErrorEncoding = System.Text.Encoding.UTF8
            };

            using var process = new Process { StartInfo = startInfo };
            process.Start();
            await process.WaitForExitAsync();
            exitCode = process.ExitCode;

            progress?.Report($"Limpeza {drive} finalizada");
        }
        catch (Exception ex)
        {
            _logger.Error($"[DiskCleanupService] Erro na limpeza do disco {drive}", ex);
            exitCode = 1;
        }
        finally
        {
            try { File.Delete(tempFile); } catch { }
        }

        return exitCode;
    }
}
