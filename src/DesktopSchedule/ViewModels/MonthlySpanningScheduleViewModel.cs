using DesktopSchedule.Models;

namespace DesktopSchedule.ViewModels;

/// <summary>
/// 월간 화면에서 사용하는 여러 날짜 연결 일정입니다.
/// 공통 일정 정보는 SpanningScheduleViewModel에서 제공하고,
/// 월간 6주 달력에서 필요한 표시 종료 날짜와 다음 주 연결 상태를 추가로 관리합니다.
/// </summary>
public class MonthlySpanningScheduleViewModel : SpanningScheduleViewModel
{
    /// <summary>
    /// 현재 주에서 연결 일정 막대가 표시되는 마지막 날짜입니다.
    /// </summary>
    public DateTime DisplayEndDate { get; }

    /// <summary>
    /// 실제 일정이 현재 표시 구간보다 이후 날짜까지 계속되는지 여부입니다.
    /// true이면 다음 주에도 일정이 이어집니다.
    /// </summary>
    public bool ContinuesToNextWeek { get; }

    public MonthlySpanningScheduleViewModel(ScheduleItem schedule, DateTime displayDate, DateTime displayEndDate, int startDayIndex, int daySpan, int rowIndex, bool continuesToNextWeek) : base(schedule, displayDate, startDayIndex, daySpan, rowIndex)
    {
        if (displayEndDate.Date < displayDate.Date)
        {
            throw new ArgumentException("표시 종료 날짜는 표시 시작 날짜보다 빠를 수 없습니다.", nameof(displayEndDate));
        }

        DisplayEndDate = displayEndDate.Date;
        ContinuesToNextWeek = continuesToNextWeek;
    }
}