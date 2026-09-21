namespace DesktopSchedule.ViewModels;

/// <summary>
/// 주간 화면에서 하루에 해당하는 날짜와 화면 상태를 관리합니다.
/// </summary>
public class WeeklyDayViewModel : ViewModelBase
{
    private bool _isSelected;
    private bool _isDropTarget;

    /// <summary>
    /// 이 항목이 나타내는 날짜입니다.
    /// </summary>
    public DateTime Date { get; }

    /// <summary>
    /// 화면에 표시할 요일 이름입니다.
    /// </summary>
    public string DayName => Date.DayOfWeek switch
    {
        DayOfWeek.Sunday => "일",
        DayOfWeek.Monday => "월",
        DayOfWeek.Tuesday => "화",
        DayOfWeek.Wednesday => "수",
        DayOfWeek.Thursday => "목",
        DayOfWeek.Friday => "금",
        DayOfWeek.Saturday => "토",
        _ => string.Empty
    };

    /// <summary>
    /// 화면에 표시할 날짜 숫자입니다.
    /// </summary>
    public string DayText => Date.Day.ToString();

    /// <summary>
    /// 이 날짜가 오늘인지 여부입니다.
    /// </summary>
    public bool IsToday => Date == DateTime.Today;

    /// <summary>
    /// 사용자가 현재 선택한 날짜인지 여부입니다.
    /// </summary>
    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    /// <summary>
    /// Drag 중인 일정이 현재 날짜에 Drop될 예정인지 나타냅니다.
    /// </summary>
    public bool IsDropTarget
    {
        get => _isDropTarget;
        set => SetProperty(ref _isDropTarget, value);
    }

    public WeeklyDayViewModel(DateTime date)
    {
        Date = date.Date;
    }
}