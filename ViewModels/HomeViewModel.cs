using System.Windows.Input;
using WindowsMaintenanceCenter.Models;
using WindowsMaintenanceCenter.Services;

namespace WindowsMaintenanceCenter.ViewModels;

public class HomeViewModel : ViewModelBase
{
    private readonly DiagnosticService _diagnosticService;
    private readonly ConfigService _configService;
    private readonly HistoryService _historyService;
    private readonly MainViewModel _mainViewModel;
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

    public ICommand RefreshCommand { get; }
    public ICommand RunOptimizationCommand { get; }

    public HomeViewModel(DiagnosticService diagnosticService, ConfigService configService, HistoryService historyService, MainViewModel mainViewModel)
    {
        _diagnosticService = diagnosticService;
        _configService = configService;
        _historyService = historyService;
        _mainViewModel = mainViewModel;

        RefreshCommand = new RelayCommand(LoadData);
        RunOptimizationCommand = new RelayCommand(RunOptimization);

        LoadData();
    }

    private void LoadData()
    {
        SystemInfo = _diagnosticService.GetSystemInfo();
        Recommendations = _diagnosticService.GetRecommendations();
    }

    private void RunOptimization()
    {
        _mainViewModel.RunDailyOptimizationRequested();
    }
}