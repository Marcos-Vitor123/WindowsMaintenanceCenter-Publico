namespace WindowsMaintenanceCenter.Models;

public class HistoryEntry
{
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public string FunctionName { get; set; } = string.Empty;
    public string Command { get; set; } = string.Empty;
    public TimeSpan Duration { get; set; }
    public bool Success { get; set; }
    public int ExitCode { get; set; }
    public long SpaceFreedBytes { get; set; }
    public string? ErrorMessage { get; set; }
}
