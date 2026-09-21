using DesktopSchedule.Models;

namespace DesktopSchedule.ViewModels.Layout;

/// <summary>
/// 주간 화면에서 날짜별 일정 블록의 위치와 겹침 배치를 계산합니다.
/// </summary>
public class WeeklyScheduleLayoutCalculator
{
    // 매우 짧은 일정도 제목을 확인할 수 있도록 적용하는 최소 높이입니다.
    private const double MinimumScheduleHeight = 18.0;

    // 하루 열 하나의 너비입니다.
    private const double DayColumnWidth = 140.0;

    // 일정 블록의 좌우 여백입니다.
    private const double ScheduleHorizontalMargin = 2.0;

    // 겹치는 일정 블록 사이의 간격입니다.
    private const double ScheduleColumnGap = 2.0;

    private readonly WeeklyTimelineLayoutCalculator _timelineLayoutCalculator;

    public WeeklyScheduleLayoutCalculator(WeeklyTimelineLayoutCalculator timelineLayoutCalculator)
    {
        _timelineLayoutCalculator = timelineLayoutCalculator ?? throw new ArgumentNullException(nameof(timelineLayoutCalculator));
    }

    /// <summary>
    /// 지정한 날짜에 표시할 하루 ViewModel을 구성합니다.
    /// 하루 종일 일정과 시간 일정 조각을 모두 포함합니다.
    /// </summary>
    public WeeklyDayViewModel CreateDay(DateTime date, bool isSelected, IReadOnlyList<ScheduleItem> schedules, IReadOnlyList<WeeklyTimelineSegmentViewModel> timelineSegments)
    {
        var day = new WeeklyDayViewModel(date)
        {
            IsSelected = isSelected
        };

        AddAllDaySchedules(day, schedules);
        AddTimedSchedules(day, schedules, timelineSegments);

        return day;
    }

    /// <summary>
    /// 지정한 날짜에 포함되는 하루 종일 일정을 날짜 헤더용 목록에 추가합니다.
    /// </summary>
    private static void AddAllDaySchedules(WeeklyDayViewModel day, IReadOnlyList<ScheduleItem> schedules)
    {
        var allDaySchedules = schedules
            .Where(schedule => schedule.IsAllDay && IsScheduleOnDate(schedule, day.Date))
            .OrderBy(schedule => schedule.StartAt)
            .ThenBy(schedule => schedule.Title)
            .ToList();

        foreach (var schedule in allDaySchedules)
        {
            day.AllDaySchedules.Add(schedule);
        }
    }

    /// <summary>
    /// 지정한 날짜에 표시할 모든 시간 일정 조각을 생성하고 겹침 배치를 계산합니다.
    /// </summary>
    private void AddTimedSchedules(WeeklyDayViewModel day, IReadOnlyList<ScheduleItem> schedules, IReadOnlyList<WeeklyTimelineSegmentViewModel> timelineSegments)
    {
        var timedSchedules = new List<WeeklyTimedScheduleViewModel>();

        foreach (var schedule in schedules)
        {
            if (TryCreateTimedSchedule(schedule, day.Date, timelineSegments, out var timedSchedule))
            {
                timedSchedules.Add(timedSchedule);
            }
        }

        foreach (var timedSchedule in timedSchedules.OrderBy(schedule => schedule.StartMinutes).ThenBy(schedule => schedule.EndMinutes))
        {
            day.TimedSchedules.Add(timedSchedule);
        }

        ApplyOverlapLayouts(day.TimedSchedules.ToList());
    }

    /// <summary>
    /// 하나의 실제 일정을 지정한 날짜에 표시할 화면용 일정 조각으로 변환합니다.
    /// </summary>
    private bool TryCreateTimedSchedule(ScheduleItem schedule, DateTime date, IReadOnlyList<WeeklyTimelineSegmentViewModel> timelineSegments, out WeeklyTimedScheduleViewModel timedSchedule)
    {
        timedSchedule = null!;

        if (!_timelineLayoutCalculator.TryGetTimedSegmentMinutes(schedule, date, out var startMinutes, out var endMinutes))
        {
            return false;
        }

        var top = _timelineLayoutCalculator.GetTimelineOffset(timelineSegments, startMinutes);
        var bottom = _timelineLayoutCalculator.GetTimelineOffset(timelineSegments, endMinutes);
        var height = Math.Max(bottom - top, MinimumScheduleHeight);

        timedSchedule = new WeeklyTimedScheduleViewModel(schedule, date, startMinutes, endMinutes, top, height);

        return true;
    }

    /// <summary>
    /// 같은 날짜에서 시간이 서로 연결되는 일정들을 겹침 그룹으로 나눕니다.
    /// </summary>
    private void ApplyOverlapLayouts(List<WeeklyTimedScheduleViewModel> schedules)
    {
        if (schedules.Count == 0)
        {
            return;
        }

        var orderedSchedules = schedules.OrderBy(schedule => schedule.StartMinutes).ThenBy(schedule => schedule.EndMinutes).ToList();
        var currentGroup = new List<WeeklyTimedScheduleViewModel>();
        var currentGroupEndMinutes = -1;

        foreach (var schedule in orderedSchedules)
        {
            if (currentGroup.Count == 0)
            {
                currentGroup.Add(schedule);
                currentGroupEndMinutes = schedule.EndMinutes;
                continue;
            }

            if (schedule.StartMinutes < currentGroupEndMinutes)
            {
                currentGroup.Add(schedule);
                currentGroupEndMinutes = Math.Max(currentGroupEndMinutes, schedule.EndMinutes);
                continue;
            }

            ApplyOverlapGroupLayout(currentGroup);

            currentGroup.Clear();
            currentGroup.Add(schedule);
            currentGroupEndMinutes = schedule.EndMinutes;
        }

        ApplyOverlapGroupLayout(currentGroup);
    }

    /// <summary>
    /// 하나의 겹침 그룹 안에서 일정별 가로 열과 너비를 계산합니다.
    /// </summary>
    private static void ApplyOverlapGroupLayout(List<WeeklyTimedScheduleViewModel> group)
    {
        if (group.Count == 0)
        {
            return;
        }

        var columnEndMinutes = new List<int>();
        var columnAssignments = new Dictionary<WeeklyTimedScheduleViewModel, int>();

        foreach (var schedule in group.OrderBy(schedule => schedule.StartMinutes).ThenBy(schedule => schedule.EndMinutes))
        {
            var columnIndex = FindAvailableColumn(columnEndMinutes, schedule.StartMinutes);

            if (columnIndex == columnEndMinutes.Count)
            {
                columnEndMinutes.Add(schedule.EndMinutes);
            }
            else
            {
                columnEndMinutes[columnIndex] = schedule.EndMinutes;
            }

            columnAssignments[schedule] = columnIndex;
        }

        var columnCount = Math.Max(1, columnEndMinutes.Count);
        var totalGapWidth = ScheduleColumnGap * (columnCount - 1);
        var availableWidth = DayColumnWidth - ScheduleHorizontalMargin * 2 - totalGapWidth;
        var columnWidth = availableWidth / columnCount;

        foreach (var schedule in group)
        {
            var columnIndex = columnAssignments[schedule];
            var left = ScheduleHorizontalMargin + columnIndex * (columnWidth + ScheduleColumnGap);

            schedule.ApplyOverlapLayout(left, columnWidth, columnIndex, columnCount);
        }
    }

    /// <summary>
    /// 현재 일정이 사용할 수 있는 가장 왼쪽의 비어 있는 열을 찾습니다.
    /// </summary>
    private static int FindAvailableColumn(List<int> columnEndMinutes, int startMinutes)
    {
        for (var index = 0; index < columnEndMinutes.Count; index++)
        {
            if (columnEndMinutes[index] <= startMinutes)
            {
                return index;
            }
        }

        return columnEndMinutes.Count;
    }

    /// <summary>
    /// 하루 종일 일정이 지정한 날짜에 포함되는지 확인합니다.
    /// </summary>
    private static bool IsScheduleOnDate(ScheduleItem schedule, DateTime date)
    {
        return schedule.StartAt.Date <= date.Date && schedule.EndAt.Date >= date.Date;
    }
}