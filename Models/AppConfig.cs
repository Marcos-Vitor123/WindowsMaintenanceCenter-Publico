namespace WindowsMaintenanceCenter.Models;

public class AppConfig
{
    public bool StartWithWindows { get; set; } = true;
    public bool MinimizeToTray { get; set; } = true;
    public bool OpenCentered { get; set; } = true;
    public bool ShowNotifications { get; set; } = true;
    public bool AutoMode { get; set; } = false;

    public List<string> SelectedDrives { get; set; } = new() { "C:" };

    public Dictionary<string, TaskSchedule> TaskSchedules { get; set; } = new()
    {
        ["TempFiles"] = new TaskSchedule { Enabled = true, IntervalDays = 1 },
        ["Prefetch"] = new TaskSchedule { Enabled = true, IntervalDays = 30 },
        ["RecycleBin"] = new TaskSchedule { Enabled = true, IntervalDays = 3 },
        ["DiskCleanup"] = new TaskSchedule { Enabled = true, IntervalDays = 7 },
        ["SystemRepair"] = new TaskSchedule { Enabled = true, IntervalDays = 30 }
    };
}

public class TaskSchedule
{
    public bool Enabled { get; set; } = true;
    public int IntervalDays { get; set; } = 7;
    public string Description { get; set; } = string.Empty;
}