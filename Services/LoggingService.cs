using System.Diagnostics;
using System.IO;

namespace WindowsMaintenanceCenter.Services;

public class LoggingService
{
    private readonly string _logDir;
    private readonly object _lock = new();

    public LoggingService()
    {
        _logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
        Directory.CreateDirectory(_logDir);
    }

    public void Info(string message) => Write("INFO", message);
    public void Warn(string message) => Write("WARN", message);
    public void Error(string message, Exception? ex = null)
    {
        var text = ex != null
            ? $"{message}\n  Exception: {ex.GetType().Name}: {ex.Message}\n  StackTrace: {ex.StackTrace}"
            : message;
        Write("ERROR", text);
    }

    private void Write(string level, string message)
    {
        try
        {
            var file = Path.Combine(_logDir, $"log_{DateTime.Now:yyyyMMdd}.txt");
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            var line = $"[{timestamp}] [{level}] {message}\n";

            lock (_lock)
            {
                File.AppendAllText(file, line, System.Text.Encoding.UTF8);
            }

            Debug.WriteLine(line.TrimEnd());
        }
        catch
        {
            Debug.WriteLine($"[LoggingService] Falha ao gravar log: {message}");
        }
    }
}
