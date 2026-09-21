using DesktopSchedule.Commands; // RelayCommand를 사용하기 위해 필요합니다.
using DesktopSchedule.Services; // ScheduleService를 사용하기 위해 필요합니다.

namespace DesktopSchedule.ViewModels;

/// <summary>
/// MainWindow에 표시되는 상태와 화면 전환 동작을 관리합니다.
/// </summary>
public class MainWindowViewModel : ViewModelBase
{
    // 일정 관련 기능을 화면 ViewModel에 전달하기 위해 저장합니다.
    private readonly ScheduleService _scheduleService;

    // MainWindow의 제목을 저장합니다.
    private string _title = "DesktopSchedule";

    // MainWindow를 다른 창보다 항상 위에 표시할지 저장합니다.
    private bool _isTopmost;

    // MainWindow를 Windows 작업 표시줄에 표시할지 저장합니다.
    private bool _showInTaskbar = true;

    // 현재 MainWindow에 표시할 화면의 ViewModel을 저장합니다.
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

    /// <summary>
    /// 월간 달력 화면으로 이동하는 Command입니다.
    /// </summary>
    public RelayCommand ShowMonthlyCalendarCommand { get; }

    /// <summary>
    /// 주간 일정 화면으로 이동하는 Command입니다.
    /// </summary>
    public RelayCommand ShowWeeklyScheduleCommand { get; }

    /// <summary>
    /// 할 일 화면으로 이동하는 Command입니다.
    /// </summary>
    public RelayCommand ShowTodoCommand { get; }

    /// <summary>
    /// 설정 화면으로 이동하는 Command입니다.
    /// </summary>
    public RelayCommand ShowSettingsCommand { get; }

    public MainWindowViewModel(ScheduleService scheduleService)
    {
        _scheduleService = scheduleService ?? throw new ArgumentNullException(nameof(scheduleService));

        _currentViewModel = new MonthlyCalendarViewModel(_scheduleService);

        ShowMonthlyCalendarCommand = new RelayCommand(_ => ShowMonthlyCalendar());
        ShowWeeklyScheduleCommand = new RelayCommand(_ => ShowWeeklySchedule());
        ShowTodoCommand = new RelayCommand(_ => ShowTodo());
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
    /// 할 일 화면으로 전환합니다.
    /// </summary>
    private void ShowTodo()
    {
        CurrentViewModel = new TodoViewModel();
    }

    /// <summary>
    /// 설정 화면으로 전환합니다.
    /// </summary>
    private void ShowSettings()
    {
        CurrentViewModel = new SettingsViewModel();
    }
}