using System.Collections.ObjectModel; // 날짜별 일정 목록을 관리하기 위해 필요합니다.
using DesktopSchedule.Models; // ScheduleItem을 사용하기 위해 필요합니다.

namespace DesktopSchedule.ViewModels;

/// <summary>
/// 주간 화면에서 하루에 해당하는 날짜와 일정 목록을 관리합니다.
/// </summary>
public class WeeklyDayViewModel : ViewModelBase
{
    // 현재 날짜가 사용자가 선택한 날짜인지 저장합니다.
    private bool _isSelected;

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
    /// 날짜 헤더 아래에 작은 배지로 표시할 하루 종일 일정입니다.
    /// </summary>
    public ObservableCollection<ScheduleItem> AllDaySchedules { get; } = new();

    /// <summary>
    /// 이 날짜의 시간표에 배치할 시간 일정 조각입니다.
    /// </summary>
    public ObservableCollection<WeeklyTimedScheduleViewModel> TimedSchedules { get; } = new();

    public WeeklyDayViewModel(DateTime date)
    {
        Date = date.Date;
    }
}