using DesktopSchedule.Models;

namespace DesktopSchedule.ViewModels;

/// <summary>
/// 월간 달력에서 여러 날짜에 걸쳐 표시되는 연결 일정 막대 하나를 나타냅니다.
/// 실제 일정이 여러 주에 걸치는 경우에는 한 주에 하나씩 여러 개의 표시 막대로 나뉘지만,
/// 모든 막대는 동일한 ScheduleItem을 참조합니다.
/// </summary>
public class MonthlySpanningScheduleViewModel
{
    /// <summary>
    /// 실제 일정 데이터입니다.
    /// 일정 수정, 삭제, 완료 처리 및 Drag 이동 시 이 데이터를 사용합니다.
    /// </summary>
    public ScheduleItem Schedule { get; }

    /// <summary>
    /// 현재 주에서 이 일정 막대가 실제로 표시되기 시작하는 날짜입니다.
    /// 일정이 이전 주에서 이어져 온 경우에는 해당 주의 일요일이 될 수 있습니다.
    /// </summary>
    public DateTime DisplayDate { get; }

    /// <summary>
    /// 현재 주에서 이 일정 막대가 표시되는 마지막 날짜입니다.
    /// </summary>
    public DateTime DisplayEndDate { get; }

    /// <summary>
    /// 일요일을 0으로 했을 때 일정 막대가 시작되는 열 번호입니다.
    /// 값의 범위는 0부터 6까지입니다.
    /// </summary>
    public int StartDayIndex { get; }

    /// <summary>
    /// 일정 막대가 현재 주에서 차지하는 날짜 열 개수입니다.
    /// 예를 들어 화요일부터 목요일까지 표시되면 3입니다.
    /// </summary>
    public int DaySpan { get; }

    /// <summary>
    /// 같은 주에 여러 연결 일정이 겹칠 때 사용할 세로 행 번호입니다.
    /// 0이면 가장 위쪽 행이고 값이 커질수록 아래쪽에 배치됩니다.
    /// </summary>
    public int RowIndex { get; }

    /// <summary>
    /// 일정이 완료 상태인지 여부입니다.
    /// 완료 일정을 표시할 때 별도의 화면 스타일을 적용하는 데 사용할 수 있습니다.
    /// </summary>
    public bool IsCompleted => Schedule.IsCompleted;

    /// <summary>
    /// 실제 일정이 현재 표시 막대보다 이전 날짜에서 시작되었는지 여부입니다.
    /// true이면 이전 주에서 이어져 온 일정입니다.
    /// </summary>
    public bool ContinuesFromPreviousWeek => Schedule.StartAt.Date < DisplayDate;

    /// <summary>
    /// 실제 일정이 현재 표시 막대보다 이후 날짜까지 계속되는지 여부입니다.
    /// true이면 다음 주에도 이어지는 일정입니다.
    /// </summary>
    public bool ContinuesToNextWeek { get; }

    /// <summary>
    /// 연결 일정 막대 안에 표시할 문자열입니다.
    /// 일정의 실제 시작 날짜가 보이는 구간이면 시작 시간을 함께 표시하고,
    /// 이전 주에서 이어진 시간 일정이면 "계속"으로 표시합니다.
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
    /// 마우스를 올렸을 때 일정 전체 기간을 표시합니다.
    /// 화면에서 여러 주로 분할되어 있어도 실제 ScheduleItem의 전체 기간을 보여줍니다.
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

    /// <summary>
    /// 월간 달력의 한 주 안에 표시할 연결 일정 막대를 생성합니다.
    /// </summary>
    public MonthlySpanningScheduleViewModel(
        ScheduleItem schedule,
        DateTime displayDate,
        DateTime displayEndDate,
        int startDayIndex,
        int daySpan,
        int rowIndex,
        bool continuesToNextWeek)
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

        if (displayEndDate.Date < displayDate.Date)
        {
            throw new ArgumentException("표시 종료 날짜는 표시 시작 날짜보다 빠를 수 없습니다.", nameof(displayEndDate));
        }

        DisplayDate = displayDate.Date;
        DisplayEndDate = displayEndDate.Date;
        StartDayIndex = startDayIndex;
        DaySpan = daySpan;
        RowIndex = rowIndex;
        ContinuesToNextWeek = continuesToNextWeek;
    }
}