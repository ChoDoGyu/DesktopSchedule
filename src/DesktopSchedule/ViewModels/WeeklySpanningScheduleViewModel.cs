using DesktopSchedule.Models;

namespace DesktopSchedule.ViewModels;

/// <summary>
/// 주간 화면에서 사용하는 여러 날짜 연결 일정입니다.
/// 공통 일정 정보는 SpanningScheduleViewModelBase에서 제공하고,
/// 현재 주간 UI에서 필요한 Canvas 픽셀 위치만 추가로 제공합니다.
/// </summary>
public class WeeklySpanningScheduleViewModel : SpanningScheduleViewModelBase
{
    // 현재 주간 화면은 하루 열을 140px로 고정하여 사용하고 있습니다.
    // 이 픽셀 의존성은 이후 R5에서 가변 폭 Panel 구조로 변경하면서 제거할 예정입니다.
    private const double DayColumnWidth = 140.0;

    // 연결 일정 막대의 좌우 여백입니다.
    private const double HorizontalMargin = 4.0;

    // 연결 일정 한 행이 차지하는 세로 높이입니다.
    private const double RowHeight = 31.0;

    // 실제 일정 막대의 높이입니다.
    private const double ScheduleHeight = 28.0;

    /// <summary>
    /// Canvas에서 연결 일정 막대가 시작하는 X 위치입니다.
    /// </summary>
    public double Left => StartDayIndex * DayColumnWidth + HorizontalMargin;

    /// <summary>
    /// Canvas에서 연결 일정 막대가 시작하는 Y 위치입니다.
    /// </summary>
    public double Top => RowIndex * RowHeight + 2.0;

    /// <summary>
    /// 연결 일정 막대의 가로 너비입니다.
    /// </summary>
    public double Width => DaySpan * DayColumnWidth - HorizontalMargin * 2;

    /// <summary>
    /// 연결 일정 막대의 높이입니다.
    /// </summary>
    public double Height => ScheduleHeight;

    public WeeklySpanningScheduleViewModel(ScheduleItem schedule, DateTime displayDate, int startDayIndex, int daySpan, int rowIndex) : base(schedule, displayDate, startDayIndex, daySpan, rowIndex)
    {
    }
}