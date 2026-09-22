using DesktopSchedule.Models;

namespace DesktopSchedule.Utilities;

/// <summary>
/// 주간과 월간 달력에서 공통으로 사용하는 일정 표시 계산을 제공합니다.
/// 일정이 어느 날짜에 보이는지, 한 주에서 어느 범위를 차지하는지,
/// 다른 일정과 겹치지 않도록 어느 행에 배치할지를 계산합니다.
/// </summary>
/// <remarks>
/// 이 클래스는 화면 상태를 가지지 않는 순수 계산 전용 클래스입니다.
/// 주간과 월간 화면이 같은 일정 표시 및 배치 규칙을 사용하도록 하는 것이 목적입니다.
/// </remarks>
public static class ScheduleCalendarCalculator
{
    /// <summary>
    /// 일정이 달력에서 실제로 마지막으로 차지해야 하는 날짜를 반환합니다.
    /// </summary>
    /// <remarks>
    /// 하루 종일 일정은 종료 날짜까지 포함합니다.
    ///
    /// 시간 일정이 정확히 다음 날 00:00에 끝나는 경우에는
    /// 해당 다음 날짜를 차지하지 않습니다.
    ///
    /// 예:
    /// 9월 21일 23:00 ~ 9월 22일 00:00
    /// → 마지막 표시 날짜는 9월 21일입니다.
    ///
    /// 9월 21일 23:00 ~ 9월 22일 00:01
    /// → 마지막 표시 날짜는 9월 22일입니다.
    /// </remarks>
    public static DateTime GetLastDisplayDate(ScheduleItem schedule)
    {
        ArgumentNullException.ThrowIfNull(schedule);

        if (schedule.IsAllDay)
        {
            return schedule.EndAt.Date;
        }

        if (schedule.EndAt > schedule.StartAt && schedule.EndAt.TimeOfDay == TimeSpan.Zero)
        {
            return schedule.EndAt.Date.AddDays(-1);
        }

        return schedule.EndAt.Date;
    }

    /// <summary>
    /// 지정한 일정이 특정 날짜에 실제로 포함되는지 확인합니다.
    /// </summary>
    public static bool IsScheduleOnDate(ScheduleItem schedule, DateTime date)
    {
        ArgumentNullException.ThrowIfNull(schedule);

        var targetDate = date.Date;

        if (schedule.IsAllDay)
        {
            return schedule.StartAt.Date <= targetDate && schedule.EndAt.Date >= targetDate;
        }

        var dayStart = targetDate;
        var dayEnd = dayStart.AddDays(1);

        return schedule.StartAt < dayEnd && schedule.EndAt > dayStart;
    }

    /// <summary>
    /// 지정한 한 주에 표시해야 하는 모든 일정의 위치와 겹침 행을 계산합니다.
    /// 단일 날짜 일정과 여러 날짜 일정 모두 동일한 행 배치 규칙을 사용합니다.
    /// </summary>
    /// <param name="schedules">표시 대상으로 사용할 전체 일정 목록입니다.</param>
    /// <param name="weekStartDate">해당 주의 시작 날짜입니다. 일요일을 기준으로 사용합니다.</param>
    /// <returns>한 주 안에 표시할 일정 배치 결과와 필요한 행 개수를 반환합니다.</returns>
    public static CalendarWeekLayout CreateWeekLayout(IReadOnlyList<ScheduleItem> schedules, DateTime weekStartDate)
    {
        ArgumentNullException.ThrowIfNull(schedules);

        var normalizedWeekStart = weekStartDate.Date;
        var weekEndDate = normalizedWeekStart.AddDays(6);
        var candidates = new List<CalendarScheduleCandidate>();

        foreach (var schedule in schedules)
        {
            var scheduleStartDate = schedule.StartAt.Date;
            var scheduleEndDate = GetLastDisplayDate(schedule);

            if (scheduleEndDate < normalizedWeekStart || scheduleStartDate > weekEndDate)
            {
                continue;
            }

            var visibleStartDate = scheduleStartDate < normalizedWeekStart ? normalizedWeekStart : scheduleStartDate;
            var visibleEndDate = scheduleEndDate > weekEndDate ? weekEndDate : scheduleEndDate;
            var startDayIndex = (visibleStartDate - normalizedWeekStart).Days;
            var endDayIndex = (visibleEndDate - normalizedWeekStart).Days;

            candidates.Add(new CalendarScheduleCandidate(
                schedule,
                visibleStartDate,
                visibleEndDate,
                startDayIndex,
                endDayIndex,
                scheduleEndDate > visibleEndDate));
        }

        var orderedCandidates = candidates
            .OrderBy(candidate => candidate.StartDayIndex)
            .ThenByDescending(candidate => candidate.EndDayIndex)
            .ThenBy(candidate => candidate.Schedule.IsAllDay ? 0 : 1)
            .ThenBy(candidate => candidate.Schedule.StartAt)
            .ThenBy(candidate => candidate.Schedule.Title)
            .ToList();

        var rowEndDayIndices = new List<int>();
        var placements = new List<CalendarSchedulePlacement>();

        foreach (var candidate in orderedCandidates)
        {
            var rowIndex = FindAvailableRow(rowEndDayIndices, candidate.StartDayIndex);

            if (rowIndex == rowEndDayIndices.Count)
            {
                rowEndDayIndices.Add(candidate.EndDayIndex);
            }
            else
            {
                rowEndDayIndices[rowIndex] = candidate.EndDayIndex;
            }

            var daySpan = candidate.EndDayIndex - candidate.StartDayIndex + 1;

            placements.Add(new CalendarSchedulePlacement(
                candidate.Schedule,
                candidate.VisibleStartDate,
                candidate.VisibleEndDate,
                candidate.StartDayIndex,
                candidate.EndDayIndex,
                daySpan,
                rowIndex,
                candidate.ContinuesToNextWeek));
        }

        return new CalendarWeekLayout(placements, rowEndDayIndices.Count);
    }

    /// <summary>
    /// 일정이 사용할 수 있는 가장 위쪽의 빈 행을 찾습니다.
    /// 기존 일정과 날짜 범위가 겹치지 않으면 같은 행을 재사용합니다.
    /// </summary>
    private static int FindAvailableRow(IReadOnlyList<int> rowEndDayIndices, int startDayIndex)
    {
        for (var rowIndex = 0; rowIndex < rowEndDayIndices.Count; rowIndex++)
        {
            if (startDayIndex > rowEndDayIndices[rowIndex])
            {
                return rowIndex;
            }
        }

        return rowEndDayIndices.Count;
    }

    /// <summary>
    /// 실제 화면 배치를 만들기 전 한 주 안의 표시 범위를 계산할 때 사용하는 내부 데이터입니다.
    /// </summary>
    private readonly record struct CalendarScheduleCandidate(
        ScheduleItem Schedule,
        DateTime VisibleStartDate,
        DateTime VisibleEndDate,
        int StartDayIndex,
        int EndDayIndex,
        bool ContinuesToNextWeek);
}

/// <summary>
/// 한 주에 표시할 모든 일정의 최종 계산 결과입니다.
/// </summary>
public sealed class CalendarWeekLayout
{
    /// <summary>
    /// 현재 주에 표시할 일정들의 위치 정보입니다.
    /// </summary>
    public IReadOnlyList<CalendarSchedulePlacement> Placements { get; }

    /// <summary>
    /// 모든 일정을 겹치지 않게 표시하기 위해 필요한 행 개수입니다.
    /// </summary>
    public int RowCount { get; }

    public CalendarWeekLayout(IReadOnlyList<CalendarSchedulePlacement> placements, int rowCount)
    {
        Placements = placements ?? throw new ArgumentNullException(nameof(placements));

        if (rowCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(rowCount));
        }

        RowCount = rowCount;
    }
}

/// <summary>
/// 일정 하나가 특정 주에서 차지할 실제 표시 위치를 나타냅니다.
/// 단일 날짜 일정은 DaySpan이 1이고,
/// 여러 날짜 일정은 차지하는 날짜 수만큼 DaySpan이 증가합니다.
/// </summary>
public readonly record struct CalendarSchedulePlacement(
    ScheduleItem Schedule,
    DateTime VisibleStartDate,
    DateTime VisibleEndDate,
    int StartDayIndex,
    int EndDayIndex,
    int DaySpan,
    int RowIndex,
    bool ContinuesToNextWeek);