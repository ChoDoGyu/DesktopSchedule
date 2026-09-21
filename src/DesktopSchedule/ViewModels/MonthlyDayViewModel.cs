namespace DesktopSchedule.ViewModels;

/// <summary>
/// 월간 달력에서 날짜 한 칸의 상태를 관리합니다.
/// 날짜 자체의 정보와 현재 월 여부, 오늘 여부, 선택 및 Drag Drop 상태를 제공합니다.
/// </summary>
public class MonthlyDayViewModel : ViewModelBase
{
    // 사용자가 현재 선택한 날짜인지 여부입니다.
    private bool _isSelected;

    // Drag 중인 일정이 이 날짜에 Drop될 예정인지 여부입니다.
    private bool _isDropTarget;

    /// <summary>
    /// 이 날짜 셀이 나타내는 실제 날짜입니다.
    /// 시간 정보는 제거하고 날짜만 보관합니다.
    /// </summary>
    public DateTime Date { get; }

    /// <summary>
    /// 이 날짜가 현재 화면에서 보고 있는 월에 속하는지 여부입니다.
    /// false이면 이전 달 또는 다음 달에서 함께 표시되는 날짜입니다.
    /// </summary>
    public bool IsCurrentMonth { get; }

    /// <summary>
    /// 이 날짜가 오늘인지 여부입니다.
    /// </summary>
    public bool IsToday => Date == DateTime.Today;

    /// <summary>
    /// 이 날짜가 일요일인지 여부입니다.
    /// 월간 달력에서 일요일 표시 스타일을 구분할 때 사용합니다.
    /// </summary>
    public bool IsSunday => Date.DayOfWeek == DayOfWeek.Sunday;

    /// <summary>
    /// 이 날짜가 토요일인지 여부입니다.
    /// 월간 달력에서 토요일 표시 스타일을 구분할 때 사용합니다.
    /// </summary>
    public bool IsSaturday => Date.DayOfWeek == DayOfWeek.Saturday;

    /// <summary>
    /// 날짜 셀의 오른쪽 위에 표시할 날짜 문자열입니다.
    /// 이전 달 또는 다음 달의 1일은 어느 달인지 알 수 있도록 월까지 함께 표시합니다.
    /// </summary>
    public string DayText
    {
        get
        {
            if (!IsCurrentMonth && Date.Day == 1)
            {
                return $"{Date.Month}월 {Date.Day}일";
            }

            return Date.Day.ToString();
        }
    }

    /// <summary>
    /// 사용자가 현재 선택한 날짜인지 여부입니다.
    /// 선택된 날짜는 월간 달력에서 별도의 시각적 강조를 적용합니다.
    /// </summary>
    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    /// <summary>
    /// Drag 중인 일정의 현재 Drop 대상 날짜인지 여부입니다.
    /// 한 번에 하나의 날짜 셀만 Drop 대상으로 강조합니다.
    /// </summary>
    public bool IsDropTarget
    {
        get => _isDropTarget;
        set => SetProperty(ref _isDropTarget, value);
    }

    /// <summary>
    /// 날짜 셀을 생성합니다.
    /// displayMonth는 현재 월간 화면에서 보고 있는 월을 의미합니다.
    /// </summary>
    public MonthlyDayViewModel(DateTime date, DateTime displayMonth)
    {
        Date = date.Date;

        IsCurrentMonth =
            Date.Year == displayMonth.Year &&
            Date.Month == displayMonth.Month;
    }
}