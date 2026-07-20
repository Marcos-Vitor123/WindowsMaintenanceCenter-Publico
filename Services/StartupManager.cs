using System.Diagnostics;
using System.IO;
using Microsoft.Win32;
using WindowsMaintenanceCenter.Models;

namespace WindowsMaintenanceCenter.Services;

public class StartupManager
{
    private const string RunKeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
    private const string RunOnceKeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\RunOnce";
    private const string UserRunKeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";

    public List<StartupEntry> GetStartupEntries()
    {
        var entries = new List<StartupEntry>();

        // HKLM\Run
        entries.AddRange(ReadRegistryKey(Registry.LocalMachine, RunKeyPath, "HKLM"));

        // HKCU\Run
        entries.AddRange(ReadRegistryKey(Registry.CurrentUser, UserRunKeyPath, "HKCU"));

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
        const string appName = "WindowsMaintenanceCenter";
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, true);
        if (key == null) return;

        if (enabled)
        {
            var exePath = Environment.ProcessPath ?? "";
            key.SetValue(appName, $"\"{exePath}\" --minimized");
        }
        else
        {
            key.DeleteValue(appName, false);
        }
    }

    public bool IsAutoStartEnabled()
    {
        const string appName = "WindowsMaintenanceCenter";
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, false);
        return key?.GetValue(appName) != null;
    }

    public void Refresh() { } // Would check for new entries

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
        // Simple extraction - in reality would need proper parsing
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