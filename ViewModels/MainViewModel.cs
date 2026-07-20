using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using WindowsMaintenanceCenter.Models;
using WindowsMaintenanceCenter.Services;
using WindowsMaintenanceCenter.Views;

namespace WindowsMaintenanceCenter.ViewModels;

public enum PageType
{
    Home,
    Maintenance,
    Diagnostics,
    Startup,
    Settings,
    History
}

public class MainViewModel : ViewModelBase
{
    private readonly ConfigService _configService;
    private readonly DiagnosticService _diagnosticService;
    private readonly HistoryService _historyService;
    private readonly NotificationService _notificationService;
    private readonly SoundService _soundService;
    private readonly AutomationService _automationService;
    private readonly MaintenanceEngine _maintenanceEngine;
    private readonly SystemRepairEngine _repairEngine;
    private readonly DeepCleanEngine _deepCleanEngine;
    private readonly StartupManager _startupManager;
    private readonly LoggingService _logger;

    private PageType _currentPage = PageType.Home;
    private ViewModelBase? _currentViewModel;
    private string _statusText = "Pronto";
    private int _progressValue = 0;
    private bool _isIndeterminate = false;
    private bool _isRunning = false;

    public PageType CurrentPage
    {
        get => _currentPage;
        set
        {
            if (SetProperty(ref _currentPage, value))
            {
                LoadPage(value);
            }
        }
    }

    public ViewModelBase? CurrentViewModel
    {
        get => _currentViewModel;
        set => SetProperty(ref _currentViewModel, value);
    }

    public string StatusText
    {
        get => _statusText;
        set => SetProperty(ref _statusText, value);
    }

    public int ProgressValue
    {
        get => _progressValue;
        set => SetProperty(ref _progressValue, value);
    }

    public bool IsIndeterminate
    {
        get => _isIndeterminate;
        set => SetProperty(ref _isIndeterminate, value);
    }

    public bool IsRunning
    {
        get => _isRunning;
        set => SetProperty(ref _isRunning, value);
    }

    public ICommand NavigateCommand { get; }

    // Action for HomeViewModel to trigger daily optimization
    public Action RunDailyOptimizationRequested { get; }

    public MainViewModel(
        ConfigService configService,
        DiagnosticService diagnosticService,
        HistoryService historyService,
        NotificationService notificationService,
        SoundService soundService,
        AutomationService automationService,
        MaintenanceEngine maintenanceEngine,
        SystemRepairEngine repairEngine,
        DeepCleanEngine deepCleanEngine,
        StartupManager startupManager,
        LoggingService logger)
    {
        _configService = configService;
        _diagnosticService = diagnosticService;
        _historyService = historyService;
        _notificationService = notificationService;
        _soundService = soundService;
        _automationService = automationService;
        _maintenanceEngine = maintenanceEngine;
        _repairEngine = repairEngine;
        _deepCleanEngine = deepCleanEngine;
        _startupManager = startupManager;
        _logger = logger;

        NavigateCommand = new RelayCommand<PageType>(p => CurrentPage = p);
        
        // Set up the action for daily optimization
        RunDailyOptimizationRequested = () => RunDailyOptimizationAsync();
        
        LoadPage(PageType.Home);
    }

    private async void RunDailyOptimizationAsync()
    {
        try
        {
            CurrentPage = PageType.Maintenance;
            
            await Task.Delay(100);
            
            if (CurrentViewModel is MaintenanceViewModel maintenanceVM)
            {
                var dailyTask = maintenanceVM.Tasks.FirstOrDefault(t => t.Id == "DailyOptimization");
                if (dailyTask != null)
                {
                    maintenanceVM.ExecuteTaskCommand.Execute(dailyTask.Id);
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[MainViewModel] Erro na otimização diária: {ex.Message}");
        }
    }

    private void LoadPage(PageType page)
    {
        CurrentViewModel = page switch
        {
            PageType.Home => new HomeViewModel(_diagnosticService, _configService, _historyService, this),
            PageType.Maintenance => new MaintenanceViewModel(_maintenanceEngine, _repairEngine, _deepCleanEngine, _notificationService, _soundService, _configService, _historyService, _logger,
                (text, progress, running, indeterminate) =>
                {
                    StatusText = text;
                    ProgressValue = progress;
                    IsRunning = running;
                    IsIndeterminate = indeterminate;
                }),
            PageType.Diagnostics => new DiagnosticsViewModel(_diagnosticService),
            PageType.Startup => new StartupViewModel(_startupManager),
            PageType.Settings => new SettingsViewModel(_configService, _notificationService, _logger),
            PageType.History => new HistoryViewModel(_historyService, _notificationService, _logger),
            _ => new HomeViewModel(_diagnosticService, _configService, _historyService, this)
        };
    }
}

public class RelayCommand<T> : ICommand
{
    private readonly Action<T?> _execute;
    private readonly Func<T?, bool>? _canExecute;

    public RelayCommand(Action<T?> execute, Func<T?, bool>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    public bool CanExecute(object? parameter) => _canExecute?.Invoke((T?)parameter) ?? true;

    public void Execute(object? parameter) => _execute((T?)parameter);

    public event EventHandler? CanExecuteChanged;
}

public class RelayCommand : ICommand
{
    private readonly Action _execute;
    private readonly Func<bool>? _canExecute;

    public RelayCommand(Action execute, Func<bool>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;

    public void Execute(object? parameter) => _execute();

    public event EventHandler? CanExecuteChanged;

    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}