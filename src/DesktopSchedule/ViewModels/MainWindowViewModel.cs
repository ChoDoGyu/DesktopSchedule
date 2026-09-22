using DesktopSchedule.Commands;
using DesktopSchedule.Services;

namespace DesktopSchedule.ViewModels;

/// <summary>
/// MainWindow에 표시되는 상태와 화면 전환 동작을 관리합니다.
/// </summary>
public class MainWindowViewModel : ViewModelBase
{
    private readonly ScheduleService _scheduleService;
    private readonly StartupService _startupService;
    private readonly AppSettingsService _appSettingsService;

    private string _title = "DesktopSchedule";
    private ViewModelBase _currentViewModel;

    /// <summary>
    /// MainWindow에 표시할 제목입니다.
    /// </summary>
    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }

    /// <summary>
    /// MainWindow를 다른 창보다 항상 위에 표시할지 여부입니다.
    /// 현재 애플리케이션 설정 상태를 그대로 제공합니다.
    /// </summary>
    public bool IsTopmost => _appSettingsService.IsTopmost;

    /// <summary>
    /// MainWindow를 Windows 작업 표시줄에 표시할지 여부입니다.
    /// 현재 애플리케이션 설정 상태를 그대로 제공합니다.
    /// </summary>
    public bool ShowInTaskbar => _appSettingsService.ShowInTaskbar;

    /// <summary>
    /// 현재 MainWindow에 표시할 화면의 ViewModel입니다.
    /// </summary>
    public ViewModelBase CurrentViewModel
    {
        get => _currentViewModel;
        set => SetProperty(ref _currentViewModel, value);
    }

    public RelayCommand ShowMonthlyCalendarCommand { get; }

    public RelayCommand ShowWeeklyScheduleCommand { get; }

    public RelayCommand ShowDailyCommand { get; }

    public RelayCommand ShowSettingsCommand { get; }

    public MainWindowViewModel(ScheduleService scheduleService, StartupService startupService, AppSettingsService appSettingsService)
    {
        _scheduleService = scheduleService ?? throw new ArgumentNullException(nameof(scheduleService));
        _startupService = startupService ?? throw new ArgumentNullException(nameof(startupService));
        _appSettingsService = appSettingsService ?? throw new ArgumentNullException(nameof(appSettingsService));

        _appSettingsService.SettingsChanged += AppSettingsService_SettingsChanged;

        _currentViewModel = new MonthlyCalendarViewModel(_scheduleService);

        ShowMonthlyCalendarCommand = new RelayCommand(_ => ShowMonthlyCalendar());
        ShowWeeklyScheduleCommand = new RelayCommand(_ => ShowWeeklySchedule());
        ShowDailyCommand = new RelayCommand(_ => ShowDaily());
        ShowSettingsCommand = new RelayCommand(_ => ShowSettings());
    }

    /// <summary>
    /// 애플리케이션 설정이 변경되면 MainWindow에 바인딩된 표시 상태를 갱신합니다.
    /// </summary>
    private void AppSettingsService_SettingsChanged()
    {
        OnPropertyChanged(nameof(IsTopmost));
        OnPropertyChanged(nameof(ShowInTaskbar));
    }

    /// <summary>
    /// 월간 달력 화면으로 전환합니다.
    /// </summary>
    private void ShowMonthlyCalendar()
    {
        CurrentViewModel = new MonthlyCalendarViewModel(_scheduleService);
    }

    /// <summary>
    /// 주간 일정 화면으로 전환합니다.
    /// </summary>
    private void ShowWeeklySchedule()
    {
        CurrentViewModel = new WeeklyScheduleViewModel(_scheduleService);
    }

    /// <summary>
    /// 일간 화면으로 전환합니다.
    /// </summary>
    private void ShowDaily()
    {
        CurrentViewModel = new DailyViewModel(_scheduleService);
    }

    /// <summary>
    /// 설정 화면으로 전환합니다.
    /// Windows 설정 Service와 애플리케이션 설정 Service를 전달합니다.
    /// </summary>
    private void ShowSettings()
    {
        CurrentViewModel = new SettingsViewModel(_startupService, _appSettingsService);
    }
}