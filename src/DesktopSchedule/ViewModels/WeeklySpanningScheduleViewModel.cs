using DesktopSchedule.Models;

namespace DesktopSchedule.ViewModels;

/// <summary>
/// 주간 화면에서 사용하는 일정 배치 ViewModel입니다.
/// 단일 날짜 일정과 여러 날짜 일정 모두 동일한 타입으로 표현하며,
/// 공통 일정 정보와 Row 배치 정보는 SpanningScheduleViewModelBase에서 제공합니다.
/// </summary>
public class WeeklySpanningScheduleViewModel : SpanningScheduleViewModelBase
{
    public WeeklySpanningScheduleViewModel(ScheduleItem schedule, DateTime displayDate, int startDayIndex, int daySpan, int rowIndex) : base(schedule, displayDate, startDayIndex, daySpan, rowIndex)
    {
    }
}