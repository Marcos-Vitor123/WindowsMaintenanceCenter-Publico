namespace WindowsMaintenanceCenter.Models;

public class MaintenanceTask
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Command { get; set; } = string.Empty;
    public bool RequiresRestart { get; set; }
    public string Icon { get; set; } = string.Empty;
    public bool IsDeepClean { get; set; }
}