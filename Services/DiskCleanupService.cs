using System.Diagnostics;
using System.IO;
using Microsoft.Win32;

namespace WindowsMaintenanceCenter.Services;

public class DiskCleanupService
{
    private const string VolumeCachesPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches";
    private const int StateFlagsChecked = 2;
    private const int SagesetNumber = 1;

    private readonly LoggingService _logger;

    public DiskCleanupService(LoggingService logger)
    {
        _logger = logger;
    }

    public List<DriveInfo> GetAvailableDrives()
    {
        return DriveInfo.GetDrives()
            .Where(d => d.DriveType is DriveType.Fixed or DriveType.Removable
                     && d.IsReady
                     && d.Name.Length == 3)
            .OrderBy(d => d.Name)
            .ToList();
    }

    public void ConfigureSagesetSilently()
    {
        try
        {
            using var baseKey = Registry.LocalMachine.OpenSubKey(VolumeCachesPath, writable: true);
            if (baseKey == null)
            {
                _logger.Warn("[DiskCleanupService] VolumeCaches registry key not found");
                return;
            }

            foreach (var subKeyName in baseKey.GetSubKeyNames())
            {
                using var subKey = baseKey.OpenSubKey(subKeyName, writable: true);
                if (subKey == null) continue;

                var valueName = $"StateFlags{SagesetNumber:D4}";
                subKey.SetValue(valueName, StateFlagsChecked, RegistryValueKind.DWord);
            }

            _logger.Info("[DiskCleanupService] Sageset configurado silenciosamente via registro");
        }
        catch (Exception ex)
        {
            _logger.Error("[DiskCleanupService] Erro ao configurar sageset no registro", ex);
        }
    }

    public string BuildCleanMgrCommand(List<string> selectedDrives)
    {
        if (selectedDrives.Count == 0)
            selectedDrives = new List<string> { "C:" };

        var firstDrive = selectedDrives[0];
        var rest = selectedDrives.Skip(1)
            .Select(d => $" && cleanmgr /sagerun:{SagesetNumber} /D {d}")
            .FirstOrDefault() ?? "";

        return $"cleanmgr /sagerun:{SagesetNumber} /D {firstDrive}{rest}";
    }

    public async Task<int> RunCleanMgrForDrivesAsync(List<string> selectedDrives, IProgress<string>? progress = null)
    {
        ConfigureSagesetSilently();

        int lastExit = 0;
        foreach (var drive in selectedDrives)
        {
            var cmd = $"cleanmgr /sagerun:{SagesetNumber} /D {drive}";
            progress?.Report($"Executando limpeza de disco no {drive}...");
            _logger.Info($"[DiskCleanupService] Executando: {cmd}");

            lastExit = await RunCommandAsync(cmd);
            progress?.Report($"Limpeza {drive} concluída (código: {lastExit})");
        }
        return lastExit;
    }

    private async Task<int> RunCommandAsync(string command)
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
            StandardErrorEncoding = System.Text.Encoding.UTF8,
            WindowStyle = ProcessWindowStyle.Hidden
        };

        using var process = new Process { StartInfo = startInfo };
        process.Start();
        await process.WaitForExitAsync();
        return process.ExitCode;
    }
}
