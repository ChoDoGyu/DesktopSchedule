using DesktopSchedule.Commands; // RelayCommand를 사용하기 위해 필요합니다.

namespace DesktopSchedule.ViewModels;

/// <summary>
/// MainWindow에 표시되는 상태와 화면 전환 동작을 관리합니다.
/// </summary>
public class MainWindowViewModel : ViewModelBase
{
    // MainWindow의 제목을 저장합니다.
    private string _title = "DesktopSchedule";

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
    /// 현재 MainWindow에 표시할 화면의 ViewModel입니다.
    /// </summary>
    public ViewModelBase CurrentViewModel
    {
        get => _currentViewModel;

        // 현재 화면이 변경되면 PropertyChanged를 발생시켜
        // WPF가 새로운 화면을 표시할 수 있도록 합니다.
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

    public MainWindowViewModel()
    {
        // 프로그램을 처음 실행했을 때 월간 달력 화면을 기본 화면으로 사용합니다.
        _currentViewModel = new MonthlyCalendarViewModel();

        // 각 Command가 실행되었을 때 CurrentViewModel을
        // 해당 화면의 ViewModel로 변경하도록 연결합니다.
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
        CurrentViewModel = new MonthlyCalendarViewModel();
    }

    /// <summary>
    /// 주간 일정 화면으로 전환합니다.
    /// </summary>
    private void ShowWeeklySchedule()
    {
        CurrentViewModel = new WeeklyScheduleViewModel();
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