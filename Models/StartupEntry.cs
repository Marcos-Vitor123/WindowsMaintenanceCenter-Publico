namespace WindowsMaintenanceCenter.Models;

public class StartupEntry
{
    public string ProgramName { get; set; } = string.Empty;
    public string Publisher { get; set; } = string.Empty;
    public string Impact { get; set; } = "Baixo";
    public bool Enabled { get; set; }
    public string RegistryPath { get; set; } = string.Empty;
}