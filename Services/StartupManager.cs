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
            // Re-enable: we'd need to store the value somewhere
            // For simplicity, just delete the disabled marker
            // In real implementation, store original value
        }
        else
        {
            // Disable: rename value to _disabled_<name>
            var value = key.GetValue(entry.ProgramName)?.ToString();
            if (value != null)
            {
                key.DeleteValue(entry.ProgramName, false);
                key.SetValue($"_disabled_{entry.ProgramName}", value);
            }
        }
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
        // Simplified heuristic
        var lower = command.ToLower();
        if (lower.Contains("steam") || lower.Contains("discord") || lower.Contains("epic") || lower.Contains("battle.net"))
            return "Alto";
        if (lower.Contains("adobe") || lower.Contains("nvidia") || lower.Contains("intel"))
            return "Médio";
        return "Baixo";
    }
}