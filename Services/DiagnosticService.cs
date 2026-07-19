using System.Diagnostics;
using System.IO;
using System.Management;
using WindowsMaintenanceCenter.Models;

namespace WindowsMaintenanceCenter.Services;

public class DiagnosticService
{
    public SystemInfo GetSystemInfo()
    {
        var info = new SystemInfo();

        try
        {
            // Windows Version
            info.WindowsVersion = GetWindowsVersion();

            // Disk Space (C:)
            var drive = new DriveInfo("C");
            info.TotalDiskSpaceBytes = drive.TotalSize;
            info.FreeDiskSpaceBytes = drive.AvailableFreeSpace;

            // RAM
            info.TotalRamBytes = GetTotalRam();

            // Last maintenance from history (would come from HistoryService)
            // info.LastMaintenance = ...

            // Status based on free space
            var freePercent = (double)info.FreeDiskSpaceBytes / info.TotalDiskSpaceBytes * 100;
            if (freePercent < 10)
            {
                info.StatusText = "Pouco espaço em disco";
                info.StatusColor = "#e74c3c";
            }
            else if (freePercent < 20)
            {
                info.StatusText = "Atenção: espaço em disco baixo";
                info.StatusColor = "#f39c12";
            }
            else
            {
                info.StatusText = "Sistema saudável";
                info.StatusColor = "#27ae60";
            }
        }
        catch
        {
            info.StatusText = "Erro ao obter informações";
            info.StatusColor = "#e74c3c";
        }

        return info;
    }

    public List<string> GetRecommendations()
    {
        var recommendations = new List<string>();
        var info = GetSystemInfo();

        var freePercent = (double)info.FreeDiskSpaceBytes / info.TotalDiskSpaceBytes * 100;
        if (freePercent < 15)
            recommendations.Add("Espaço em disco baixo. Recomendada limpeza profunda.");

        if (info.LastOptimization == null || (DateTime.Now - info.LastOptimization.Value).TotalDays > 7)
            recommendations.Add("Otimização diária não realizada há mais de 7 dias.");

        if (info.LastRepair == null || (DateTime.Now - info.LastRepair.Value).TotalDays > 30)
            recommendations.Add("Reparação do sistema não realizada há mais de 30 dias.");

        return recommendations;
    }

    private string GetWindowsVersion()
    {
        try
        {
            using var os = new ManagementObjectSearcher("SELECT Caption, Version, BuildNumber FROM Win32_OperatingSystem").Get();
            foreach (var obj in os)
            {
                return $"{obj["Caption"]} {obj["Version"]} (Build {obj["BuildNumber"]})";
            }
        }
        catch { }
        return Environment.OSVersion.ToString();
    }

    private long GetTotalRam()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT TotalPhysicalMemory FROM Win32_ComputerSystem");
            foreach (var obj in searcher.Get())
            {
                return Convert.ToInt64(obj["TotalPhysicalMemory"]);
            }
        }
        catch { }
        return 0;
    }
}