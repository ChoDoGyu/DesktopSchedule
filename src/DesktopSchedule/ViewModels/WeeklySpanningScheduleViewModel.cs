using DesktopSchedule.Models;

namespace DesktopSchedule.ViewModels;

/// <summary>
/// 주간 화면에서 두 날짜 이상에 걸쳐 표시되는 연결 일정 막대를 관리합니다.
/// 하나의 실제 ScheduleItem을 주간 범위 안에서 필요한 부분만 잘라 표시합니다.
/// </summary>
public class WeeklySpanningScheduleViewModel
{
    private const double DayColumnWidth = 140.0;
    private const double HorizontalMargin = 4.0;
    private const double RowHeight = 31.0;
    private const double ScheduleHeight = 28.0;

    /// <summary>
    /// 실제 일정 데이터입니다.
    /// </summary>
    public ScheduleItem Schedule { get; }

    /// <summary>
    /// 현재 주에서 이 일정 막대가 시작되어 보이는 날짜입니다.
    /// 주 시작 이전부터 이어진 일정이면 주 시작 날짜가 됩니다.
    /// </summary>
    public DateTime DisplayDate { get; }

    /// <summary>
    /// 일요일을 0으로 했을 때 일정 막대가 시작하는 열 번호입니다.
    /// </summary>
    public int StartDayIndex { get; }

    /// <summary>
    /// 일정 막대가 차지하는 날짜 열 개수입니다.
    /// </summary>
    public int DaySpan { get; }

    /// <summary>
    /// 다른 여러 날짜 일정과 겹칠 때 사용할 세로 행 번호입니다.
    /// </summary>
    public int RowIndex { get; }

    /// <summary>
    /// Canvas에서 일정 막대가 시작할 X 위치입니다.
    /// </summary>
    public double Left => StartDayIndex * DayColumnWidth + HorizontalMargin;

    /// <summary>
    /// Canvas에서 일정 막대가 시작할 Y 위치입니다.
    /// </summary>
    public double Top => RowIndex * RowHeight + 2.0;

    /// <summary>
    /// 일정 막대의 가로 너비입니다.
    /// </summary>
    public double Width => DaySpan * DayColumnWidth - HorizontalMargin * 2;

    /// <summary>
    /// 일정 막대의 높이입니다.
    /// </summary>
    public double Height => ScheduleHeight;

    /// <summary>
    /// 화면에 표시할 문자열입니다.
    /// </summary>
    public string DisplayText
    {
        get
        {
            if (Schedule.IsAllDay)
            {
                return Schedule.Title;
            }

            if (DisplayDate == Schedule.StartAt.Date)
            {
                return $"{Schedule.StartAt:HH:mm} {Schedule.Title}";
            }

            return $"계속 {Schedule.Title}";
        }
    }

    /// <summary>
    /// 마우스를 올렸을 때 실제 일정 전체 기간을 표시합니다.
    /// </summary>
    public string ToolTipText
    {
        get
        {
            if (Schedule.IsAllDay)
            {
                return $"{Schedule.StartAt:yyyy-MM-dd} ~ {Schedule.EndAt:yyyy-MM-dd} · 하루 종일";
            }

            return $"{Schedule.StartAt:yyyy-MM-dd HH:mm} ~ {Schedule.EndAt:yyyy-MM-dd HH:mm}";
        }
    }

    public WeeklySpanningScheduleViewModel(ScheduleItem schedule, DateTime displayDate, int startDayIndex, int daySpan, int rowIndex)
    {
        Schedule = schedule ?? throw new ArgumentNullException(nameof(schedule));

        if (startDayIndex < 0 || startDayIndex > 6)
        {
            throw new ArgumentOutOfRangeException(nameof(startDayIndex));
        }

        if (daySpan <= 0 || startDayIndex + daySpan > 7)
        {
            throw new ArgumentOutOfRangeException(nameof(daySpan));
        }

        if (rowIndex < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(rowIndex));
        }

        DisplayDate = displayDate.Date;
        StartDayIndex = startDayIndex;
        DaySpan = daySpan;
        RowIndex = rowIndex;
    }
}