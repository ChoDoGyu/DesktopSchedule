using DesktopSchedule.Models;

namespace DesktopSchedule.ViewModels;

/// <summary>
/// 주간과 월간 달력에서 표시되는 일정의
/// 공통 상태와 Row 배치 정보를 제공합니다.
/// </summary>
/// <remarks>
/// 주간 화면에서는 이 타입을 직접 사용하고,
/// 월간 화면은 추가 정보가 필요한 경우 상속하여 확장합니다.
/// 실제 픽셀 위치 계산은 공통 CalendarSchedulePanel이 담당합니다.
/// </remarks>
public class SpanningScheduleViewModel
{
    /// <summary>
    /// 실제 일정 데이터입니다.
    /// 수정, 삭제, 완료 처리와 Drag 이동에서 사용합니다.
    /// </summary>
    public ScheduleItem Schedule { get; }

    /// <summary>
    /// 현재 주에서 일정 막대가 실제로 표시되기 시작하는 날짜입니다.
    /// 일정이 이전 주에서 시작된 경우 현재 주의 시작 날짜가 될 수 있습니다.
    /// </summary>
    public DateTime DisplayDate { get; }

    /// <summary>
    /// 일요일을 0으로 했을 때 일정이 시작하는 열 번호입니다.
    /// </summary>
    public int StartDayIndex { get; }

    /// <summary>
    /// 일정이 현재 주에서 차지하는 날짜 열 개수입니다.
    /// </summary>
    public int DaySpan { get; }

    /// <summary>
    /// 같은 주의 다른 일정과 겹칠 때 사용하는 세로 행 번호입니다.
    /// </summary>
    public int RowIndex { get; }

    /// <summary>
    /// 일정이 완료 상태인지 여부입니다.
    /// </summary>
    public bool IsCompleted => Schedule.IsCompleted;

    /// <summary>
    /// 실제 일정이 현재 표시 막대보다 이전 날짜에서 시작되었는지 여부입니다.
    /// true이면 이전 주에서 현재 주까지 이어져 온 일정입니다.
    /// </summary>
    public bool ContinuesFromPreviousWeek => Schedule.StartAt.Date < DisplayDate;

    /// <summary>
    /// 일정 막대 안에 표시할 문자열입니다.
    /// </summary>
    public string DisplayText
    {
        get
        {
            if (Schedule.IsAllDay)
            {
                return Schedule.Title;
            }

            if (!ContinuesFromPreviousWeek)
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

    public SpanningScheduleViewModel(ScheduleItem schedule, DateTime displayDate, int startDayIndex, int daySpan, int rowIndex)
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