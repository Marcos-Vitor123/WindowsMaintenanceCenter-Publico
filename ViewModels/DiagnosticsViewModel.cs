using WindowsMaintenanceCenter.Models;
using WindowsMaintenanceCenter.Services;

namespace WindowsMaintenanceCenter.ViewModels;

public class DiagnosticsViewModel : ViewModelBase
{
    private readonly DiagnosticService _diagnosticService;
    private SystemInfo? _systemInfo;
    private List<string> _recommendations = new();

    public SystemInfo? SystemInfo
    {
        get => _systemInfo;
        set => SetProperty(ref _systemInfo, value);
    }

    public List<string> Recommendations
    {
        get => _recommendations;
        set => SetProperty(ref _recommendations, value);
    }

    public DiagnosticsViewModel(DiagnosticService diagnosticService)
    {
        _diagnosticService = diagnosticService;
        LoadData();
    }

    private void LoadData()
    {
        SystemInfo = _diagnosticService.GetSystemInfo();
        Recommendations = _diagnosticService.GetRecommendations();
    }
}