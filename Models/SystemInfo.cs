namespace WindowsMaintenanceCenter.Models;

public class SystemInfo
{
    public string WindowsVersion { get; set; } = string.Empty;
    public long TotalDiskSpaceBytes { get; set; }
    public long FreeDiskSpaceBytes { get; set; }
    public long TotalRamBytes { get; set; }
    public DateTime? LastMaintenance { get; set; }
    public DateTime? LastRepair { get; set; }
    public DateTime? LastOptimization { get; set; }
    public long SpaceFreedTotalBytes { get; set; }
    public string StatusText { get; set; } = "Sistema saudável";
    public string StatusColor { get; set; } = "#27ae60";
}