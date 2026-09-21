using DesktopSchedule.Models;

namespace DesktopSchedule.Utilities;

/// <summary>
/// 주간과 월간 달력에서 공통으로 사용하는 일정 표시 계산을 제공합니다.
/// 일정이 어느 날짜에 보이는지, 여러 날짜 일정인지,
/// 한 주 안에서 어느 범위를 차지하고 어느 행에 배치할지를 계산합니다.
/// </summary>
/// <remarks>
/// 이 클래스는 화면 상태를 가지지 않는 순수 계산 전용 클래스입니다.
/// 주간과 월간 화면이 같은 일정 표시 규칙을 사용하도록 하는 것이 목적입니다.
/// </remarks>
public static class ScheduleCalendarCalculator
{
    /// <summary>
    /// 일정이 달력에서 두 개 이상의 날짜 셀을 차지하는지 확인합니다.
    /// 실제 마지막 표시 날짜가 시작 날짜보다 뒤에 있으면 연결 일정으로 판단합니다.
    /// </summary>
    public static bool IsSpanningSchedule(ScheduleItem schedule)
    {
        ArgumentNullException.ThrowIfNull(schedule);

        return GetLastDisplayDate(schedule) > schedule.StartAt.Date;
    }

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

        if (schedule.EndAt > schedule.StartAt &&
            schedule.EndAt.TimeOfDay == TimeSpan.Zero)
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

        // 하루 종일 일정은 시작 날짜와 종료 날짜를 모두 포함합니다.
        if (schedule.IsAllDay)
        {
            return schedule.StartAt.Date <= targetDate &&
                   schedule.EndAt.Date >= targetDate;
        }

        var dayStart = targetDate;
        var dayEnd = dayStart.AddDays(1);

        // 시간 일정은 해당 날짜의 00:00 ~ 다음 날 00:00 범위와
        // 실제 일정 시간이 겹치는지를 검사합니다.
        return schedule.StartAt < dayEnd &&
               schedule.EndAt > dayStart;
    }

    /// <summary>
    /// 지정한 한 주에 표시해야 하는 여러 날짜 일정의 위치와 겹침 행을 계산합니다.
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
            if (!IsSpanningSchedule(schedule))
            {
                continue;
            }

            var scheduleStartDate = schedule.StartAt.Date;
            var scheduleEndDate = GetLastDisplayDate(schedule);

            // 실제 일정이 현재 주보다 앞에서 시작했다면
            // 화면에는 현재 주의 첫 날짜부터 보이도록 잘라냅니다.
            var visibleStartDate = scheduleStartDate < normalizedWeekStart
                ? normalizedWeekStart
                : scheduleStartDate;

            // 실제 일정이 현재 주보다 뒤까지 이어진다면
            // 화면에는 현재 주의 마지막 날짜까지만 보이도록 잘라냅니다.
            var visibleEndDate = scheduleEndDate > weekEndDate
                ? weekEndDate
                : scheduleEndDate;

            // 현재 주와 전혀 겹치지 않는 일정은 표시할 필요가 없습니다.
            if (visibleStartDate > visibleEndDate)
            {
                continue;
            }

            var startDayIndex = (visibleStartDate - normalizedWeekStart).Days;
            var endDayIndex = (visibleEndDate - normalizedWeekStart).Days;

            candidates.Add(
                new CalendarScheduleCandidate(
                    schedule,
                    visibleStartDate,
                    visibleEndDate,
                    startDayIndex,
                    endDayIndex,
                    scheduleEndDate > visibleEndDate));
        }

        // 같은 날짜에 여러 일정이 시작하는 경우
        // 더 긴 일정을 먼저 배치하고,
        // 그다음 실제 시작 시간과 제목을 사용해 안정적인 순서를 유지합니다.
        var orderedCandidates = candidates
            .OrderBy(candidate => candidate.StartDayIndex)
            .ThenByDescending(candidate => candidate.EndDayIndex)
            .ThenBy(candidate => candidate.Schedule.StartAt)
            .ThenBy(candidate => candidate.Schedule.Title)
            .ToList();

        // 각 행이 현재 어느 요일 열까지 사용되고 있는지 저장합니다.
        // 예를 들어 첫 번째 행이 목요일까지 차지하고 있으면 4가 저장됩니다.
        var rowEndDayIndices = new List<int>();

        var placements = new List<CalendarSchedulePlacement>();

        foreach (var candidate in orderedCandidates)
        {
            var rowIndex = FindAvailableRow(rowEndDayIndices, candidate.StartDayIndex);

            if (rowIndex == rowEndDayIndices.Count)
            {
                // 재사용 가능한 행이 없으므로 새 행을 추가합니다.
                rowEndDayIndices.Add(candidate.EndDayIndex);
            }
            else
            {
                // 기존 빈 행을 재사용하고
                // 이 일정이 새롭게 차지하는 마지막 열로 갱신합니다.
                rowEndDayIndices[rowIndex] = candidate.EndDayIndex;
            }

            var daySpan = candidate.EndDayIndex - candidate.StartDayIndex + 1;

            placements.Add(
                new CalendarSchedulePlacement(
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
    /// 연결 일정이 사용할 수 있는 가장 위쪽의 빈 행을 찾습니다.
    /// </summary>
    private static int FindAvailableRow(IReadOnlyList<int> rowEndDayIndices, int startDayIndex)
    {
        for (var rowIndex = 0; rowIndex < rowEndDayIndices.Count; rowIndex++)
        {
            // 기존 일정이 끝난 열보다 새로운 일정이 뒤에서 시작하면
            // 두 일정이 겹치지 않으므로 같은 행을 사용할 수 있습니다.
            if (startDayIndex > rowEndDayIndices[rowIndex])
            {
                return rowIndex;
            }
        }

        // 기존 행을 사용할 수 없다면
        // 현재 행 개수가 그대로 새로운 행 번호가 됩니다.
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
/// 한 주에 표시할 여러 날짜 일정의 최종 계산 결과입니다.
/// </summary>
public sealed class CalendarWeekLayout
{
    /// <summary>
    /// 현재 주에 표시할 일정들의 위치 정보입니다.
    /// </summary>
    public IReadOnlyList<CalendarSchedulePlacement> Placements { get; }

    /// <summary>
    /// 모든 연결 일정을 겹치지 않게 표시하기 위해 필요한 행 개수입니다.
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
/// 연결 일정 하나가 특정 주에서 차지할 실제 표시 위치를 나타냅니다.
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