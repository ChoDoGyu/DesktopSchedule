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

    private string _title = "DesktopSchedule";
    private bool _isTopmost;
    private bool _showInTaskbar = true;
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
    /// </summary>
    public bool IsTopmost
    {
        get => _isTopmost;
        set => SetProperty(ref _isTopmost, value);
    }

    /// <summary>
    /// MainWindow를 Windows 작업 표시줄에 표시할지 여부입니다.
    /// </summary>
    public bool ShowInTaskbar
    {
        get => _showInTaskbar;
        set => SetProperty(ref _showInTaskbar, value);
    }

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

    public MainWindowViewModel(ScheduleService scheduleService, StartupService startupService)
    {
        _scheduleService = scheduleService ?? throw new ArgumentNullException(nameof(scheduleService));
        _startupService = startupService ?? throw new ArgumentNullException(nameof(startupService));

        _currentViewModel = new MonthlyCalendarViewModel(_scheduleService);

        ShowMonthlyCalendarCommand = new RelayCommand(_ => ShowMonthlyCalendar());
        ShowWeeklyScheduleCommand = new RelayCommand(_ => ShowWeeklySchedule());
        ShowDailyCommand = new RelayCommand(_ => ShowDaily());
        ShowSettingsCommand = new RelayCommand(_ => ShowSettings());
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
    /// 현재 MainWindow의 표시 상태를 즉시 변경할 수 있도록 자기 자신을 전달합니다.
    /// </summary>
    private void ShowSettings()
    {
        CurrentViewModel = new SettingsViewModel(_startupService, this);
    }
}