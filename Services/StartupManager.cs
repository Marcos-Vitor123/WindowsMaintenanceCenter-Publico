using System.Diagnostics;
using System.IO;
using Microsoft.Win32;
using WindowsMaintenanceCenter.Models;

namespace WindowsMaintenanceCenter.Services;

public class StartupManager
{
    private const string RunKeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
    private const string TaskName = "WindowsMaintenanceCenter";

    public List<StartupEntry> GetStartupEntries()
    {
        var entries = new List<StartupEntry>();

        entries.AddRange(ReadRegistryKey(Registry.LocalMachine, RunKeyPath, "HKLM"));
        entries.AddRange(ReadRegistryKey(Registry.CurrentUser, RunKeyPath, "HKCU"));

        return entries;
    }

    public void SetEnabled(StartupEntry entry, bool enabled)
    {
        var root = entry.RegistryPath.StartsWith("HKLM") ? Registry.LocalMachine : Registry.CurrentUser;
        var keyPath = entry.RegistryPath.Substring(entry.RegistryPath.IndexOf('\\') + 1);

        using var key = root.OpenSubKey(keyPath, true);
        if (key == null) return;

        if (enabled)
        {
            var disabledValue = key.GetValue($"_disabled_{entry.ProgramName}");
            if (disabledValue != null)
            {
                key.DeleteValue($"_disabled_{entry.ProgramName}", false);
                key.SetValue(entry.ProgramName, disabledValue);
            }
        }
        else
        {
            var value = key.GetValue(entry.ProgramName)?.ToString();
            if (value != null)
            {
                key.DeleteValue(entry.ProgramName, false);
                key.SetValue($"_disabled_{entry.ProgramName}", value);
            }
        }
    }

    public void SetAutoStart(bool enabled)
    {
        var exePath = Environment.ProcessPath ?? "";
        if (string.IsNullOrEmpty(exePath)) return;

        if (enabled)
        {
            CreateScheduledTask(exePath);
        }
        else
        {
            DeleteScheduledTask();
        }
    }

    public bool IsAutoStartEnabled()
    {
        return IsScheduledTaskCreated();
    }

    private void CreateScheduledTask(string exePath)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "schtasks.exe",
                Arguments = $"/Create /TN \"{TaskName}\" /TR \"\\\"{exePath}\\\" --minimized\" /SC ONLOGON /RL HIGHEST /F",
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using var process = Process.Start(psi);
            process?.WaitForExit(5000);
        }
        catch
        {
            // Fallback: registry-based (will show UAC)
            SetAutoStartRegistry(exePath, true);
        }
    }

    private void DeleteScheduledTask()
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "schtasks.exe",
                Arguments = $"/Delete /TN \"{TaskName}\" /F",
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using var process = Process.Start(psi);
            process?.WaitForExit(5000);
        }
        catch
        {
            SetAutoStartRegistry("", false);
        }
    }

    private bool IsScheduledTaskCreated()
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "schtasks.exe",
                Arguments = $"/Query /TN \"{TaskName}\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using var process = Process.Start(psi);
            if (process == null) return false;
            process.WaitForExit(5000);
            return process.ExitCode == 0;
        }
        catch
        {
            return IsAutoStartRegistryEnabled();
        }
    }

    private void SetAutoStartRegistry(string exePath, bool enabled)
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, true);
        if (key == null) return;

        const string appName = "WindowsMaintenanceCenter";
        if (enabled)
        {
            key.SetValue(appName, $"\"{exePath}\" --minimized");
        }
        else
        {
            key.DeleteValue(appName, false);
        }
    }

    private bool IsAutoStartRegistryEnabled()
    {
        const string appName = "WindowsMaintenanceCenter";
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, false);
        return key?.GetValue(appName) != null;
    }

    public void Refresh() { }

    private List<StartupEntry> ReadRegistryKey(RegistryKey root, string keyPath, string prefix)
    {
        var entries = new List<StartupEntry>();

        using var key = root.OpenSubKey(keyPath, false);
        if (key == null) return entries;

        foreach (var valueName in key.GetValueNames())
        {
            var value = key.GetValue(valueName)?.ToString() ?? "";
            var isDisabled = valueName.StartsWith("_disabled_");
            var displayName = isDisabled ? valueName.Substring("_disabled_".Length) : valueName;

            entries.Add(new StartupEntry
            {
                ProgramName = displayName,
                Publisher = GetPublisherFromPath(value),
                Impact = EstimateImpact(value),
                Enabled = !isDisabled,
                RegistryPath = $"{prefix}\\{keyPath}"
            });
        }

        return entries;
    }

    private string GetPublisherFromPath(string path)
    {
        try
        {
            var exePath = ExtractExePath(path);
            if (File.Exists(exePath))
            {
                var versionInfo = FileVersionInfo.GetVersionInfo(exePath);
                return versionInfo.CompanyName ?? "Desconhecido";
            }
        }
        catch { }
        return "Desconhecido";
    }

    private string ExtractExePath(string command)
    {
        var match = System.Text.RegularExpressions.Regex.Match(command, @"""([^""]+\.exe)""");
        if (match.Success) return match.Groups[1].Value;
        match = System.Text.RegularExpressions.Regex.Match(command, @"([A-Za-z]:\\[^""\s]+\.exe)");
        return match.Success ? match.Groups[1].Value : command;
    }

    private string EstimateImpact(string command)
    {
        if (command.Contains("steam", StringComparison.OrdinalIgnoreCase) ||
            command.Contains("discord", StringComparison.OrdinalIgnoreCase) ||
            command.Contains("epic", StringComparison.OrdinalIgnoreCase) ||
            command.Contains("battle.net", StringComparison.OrdinalIgnoreCase))
            return "Alto";
        if (command.Contains("adobe", StringComparison.OrdinalIgnoreCase) ||
            command.Contains("nvidia", StringComparison.OrdinalIgnoreCase) ||
            command.Contains("intel", StringComparison.OrdinalIgnoreCase))
            return "Médio";
        return "Baixo";
    }
}
