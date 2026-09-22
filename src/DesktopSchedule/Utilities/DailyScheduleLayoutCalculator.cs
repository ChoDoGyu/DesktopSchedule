using DesktopSchedule.Models;

namespace DesktopSchedule.Utilities;

/// <summary>
/// 일간 24시간 타임라인에 표시할 시간 일정의 겹침 관계와 가로 열 배치를 계산합니다.
/// 화면 상태를 가지지 않으며 일정 데이터, 표시 날짜, 타임라인 배율만 이용하는 순수 계산 전용 클래스입니다.
/// </summary>
public static class DailyScheduleLayoutCalculator
{
    /// <summary>
    /// 지정한 날짜에 표시할 시간 일정들을 시작 시각 순으로 정렬하고,
    /// 서로 겹치는 일정 그룹마다 사용할 가로 열 번호와 전체 열 개수를 계산합니다.
    /// </summary>
    public static IReadOnlyList<DailySchedulePlacement> CreateLayout(IReadOnlyList<ScheduleItem> schedules, DateTime displayDate)
    {
        ArgumentNullException.ThrowIfNull(schedules);

        var dayStart = displayDate.Date;
        var dayEnd = dayStart.AddDays(1);

        var candidates = schedules
            .Select(schedule => CreateCandidate(schedule, dayStart, dayEnd))
            .OrderBy(candidate => candidate.VisibleStartAt)
            .ThenBy(candidate => candidate.VisibleEndAt)
            .ThenBy(candidate => candidate.Schedule.Title)
            .ToList();

        var result = new List<DailySchedulePlacement>();
        var currentGroup = new List<DailyScheduleCandidate>();
        DateTime? currentGroupEndAt = null;

        foreach (var candidate in candidates)
        {
            if (currentGroup.Count > 0 && currentGroupEndAt.HasValue && candidate.VisibleStartAt >= currentGroupEndAt.Value)
            {
                AddGroupPlacements(currentGroup, result);
                currentGroup.Clear();
                currentGroupEndAt = null;
            }

            currentGroup.Add(candidate);

            if (!currentGroupEndAt.HasValue || candidate.LayoutEndAt > currentGroupEndAt.Value)
            {
                currentGroupEndAt = candidate.LayoutEndAt;
            }
        }

        if (currentGroup.Count > 0)
        {
            AddGroupPlacements(currentGroup, result);
        }

        return result;
    }

    /// <summary>
    /// 하나의 시간 겹침 그룹 안에서 각 일정이 사용할 가장 왼쪽의 빈 열을 배정합니다.
    /// </summary>
    private static void AddGroupPlacements(IReadOnlyList<DailyScheduleCandidate> group, ICollection<DailySchedulePlacement> result)
    {
        var columnEndTimes = new List<DateTime>();
        var assignments = new List<DailyScheduleColumnAssignment>();

        foreach (var candidate in group)
        {
            var columnIndex = FindAvailableColumn(columnEndTimes, candidate.VisibleStartAt);

            if (columnIndex == columnEndTimes.Count)
            {
                columnEndTimes.Add(candidate.LayoutEndAt);
            }
            else
            {
                columnEndTimes[columnIndex] = candidate.LayoutEndAt;
            }

            assignments.Add(new DailyScheduleColumnAssignment(candidate.Schedule, columnIndex));
        }

        var columnCount = columnEndTimes.Count;

        foreach (var assignment in assignments)
        {
            result.Add(new DailySchedulePlacement(assignment.Schedule, assignment.ColumnIndex, columnCount));
        }
    }

    /// <summary>
    /// 지정한 시작 시각에 사용할 수 있는 가장 왼쪽의 빈 열 번호를 반환합니다.
    /// </summary>
    private static int FindAvailableColumn(IReadOnlyList<DateTime> columnEndTimes, DateTime startAt)
    {
        for (var columnIndex = 0; columnIndex < columnEndTimes.Count; columnIndex++)
        {
            if (startAt >= columnEndTimes[columnIndex])
            {
                return columnIndex;
            }
        }

        return columnEndTimes.Count;
    }

    /// <summary>
    /// 선택 날짜 안에서 실제로 보이는 시작/종료 시간과 최소 카드 높이를 고려한
    /// 겹침 계산용 후보 데이터를 생성합니다.
    /// </summary>
    private static DailyScheduleCandidate CreateCandidate(ScheduleItem schedule, DateTime dayStart, DateTime dayEnd)
    {
        ArgumentNullException.ThrowIfNull(schedule);

        var visibleStartAt = schedule.StartAt < dayStart ? dayStart : schedule.StartAt;
        var visibleEndAt = schedule.EndAt > dayEnd ? dayEnd : schedule.EndAt;
        var actualDurationMinutes = Math.Max(0, (visibleEndAt - visibleStartAt).TotalMinutes);
        var minimumDisplayMinutes = DailyTimelineMetrics.MinimumScheduleHeight / DailyTimelineMetrics.HourHeight * 60.0;
        var layoutDurationMinutes = Math.Max(actualDurationMinutes, minimumDisplayMinutes);
        var layoutEndAt = visibleStartAt.AddMinutes(layoutDurationMinutes);

        if (layoutEndAt > dayEnd)
        {
            layoutEndAt = dayEnd;
        }

        return new DailyScheduleCandidate(schedule, visibleStartAt, visibleEndAt, layoutEndAt);
    }

    private readonly record struct DailyScheduleCandidate(ScheduleItem Schedule, DateTime VisibleStartAt, DateTime VisibleEndAt, DateTime LayoutEndAt);

    private readonly record struct DailyScheduleColumnAssignment(ScheduleItem Schedule, int ColumnIndex);
}

/// <summary>
/// 일간 타임라인에서 일정 하나가 사용할 가로 열 위치를 나타냅니다.
/// </summary>
public readonly record struct DailySchedulePlacement(ScheduleItem Schedule, int ColumnIndex, int ColumnCount);