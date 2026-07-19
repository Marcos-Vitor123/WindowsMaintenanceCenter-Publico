using WindowsMaintenanceCenter.Models;

namespace WindowsMaintenanceCenter.Services;

public interface ILogger
{
    void Log(string message);
}