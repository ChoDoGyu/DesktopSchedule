using DesktopSchedule.Models;
using DesktopSchedule.Utilities;

namespace DesktopSchedule.ViewModels;

/// <summary>
/// 일간 화면에서 사용하는 하나의 일정 표시 정보를 제공합니다.
/// 24시간 타임라인의 위치 계산과 오른쪽 일정 목록 표시에서 함께 사용합니다.
/// </summary>
public class DailyScheduleViewModel
{
    /// <summary>
    /// 실제 일정 데이터입니다.
    /// </summary>
    public ScheduleItem Schedule { get; }

    /// <summary>
    /// 선택 날짜 안에서 실제로 표시되는 시작 시각입니다.
    /// 이전 날짜에서 이어진 일정이면 00:00이 될 수 있습니다.
    /// </summary>
    public DateTime VisibleStartAt { get; }

    /// <summary>
    /// 선택 날짜 안에서 실제로 표시되는 종료 시각입니다.
    /// 다음 날짜까지 이어지는 일정이면 다음 날 00:00이 될 수 있습니다.
    /// </summary>
    public DateTime VisibleEndAt { get; }

    /// <summary>
    /// 타임라인 위에서 일정 카드가 시작되는 Y 위치입니다.
    /// </summary>
    public double Top { get; }

    /// <summary>
    /// 일정 카드의 화면 높이입니다.
    /// </summary>
    public double Height { get; }

    /// <summary>
    /// 시간이 겹치는 일정 그룹 안에서 사용할 가로 열 번호입니다.
    /// </summary>
    public int ColumnIndex { get; }

    /// <summary>
    /// 현재 겹침 그룹에서 필요한 전체 가로 열 개수입니다.
    /// </summary>
    public int ColumnCount { get; }

    /// <summary>
    /// 하루 종일 일정인지 여부입니다.
    /// </summary>
    public bool IsAllDay => Schedule.IsAllDay;

    /// <summary>
    /// 완료된 일정인지 여부입니다.
    /// </summary>
    public bool IsCompleted => Schedule.IsCompleted;

    /// <summary>
    /// 일정 카드 또는 일정 목록에 표시할 문자열입니다.
    /// </summary>
    public string DisplayText
    {
        get
        {
            if (Schedule.IsAllDay)
            {
                return Schedule.Title;
            }

            if (Schedule.StartAt < VisibleStartAt)
            {
                return $"계속 {Schedule.Title}";
            }

            return $"{Schedule.StartAt:HH:mm} {Schedule.Title}";
        }
    }

    /// <summary>
    /// 일정 전체 기간을 Tooltip에 표시합니다.
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

    public DailyScheduleViewModel(ScheduleItem schedule, DateTime displayDate, int columnIndex, int columnCount)
    {
        Schedule = schedule ?? throw new ArgumentNullException(nameof(schedule));

        if (columnIndex < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(columnIndex));
        }

        if (columnCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(columnCount));
        }

        if (columnIndex >= columnCount)
        {
            throw new ArgumentOutOfRangeException(nameof(columnIndex));
        }

        ColumnIndex = columnIndex;
        ColumnCount = columnCount;

        var dayStart = displayDate.Date;
        var dayEnd = dayStart.AddDays(1);

        VisibleStartAt = schedule.StartAt < dayStart
            ? dayStart
            : schedule.StartAt;

        VisibleEndAt = schedule.EndAt > dayEnd
            ? dayEnd
            : schedule.EndAt;

        var startMinutes =
            (VisibleStartAt - dayStart).TotalMinutes;

        var durationMinutes = Math.Max(
            0,
            (VisibleEndAt - VisibleStartAt).TotalMinutes);

        Top = DailyTimelineMetrics.MinutesToPixels(startMinutes);

        var requestedHeight = Math.Max(
            DailyTimelineMetrics.MinimumScheduleHeight,
            DailyTimelineMetrics.MinutesToPixels(durationMinutes));

        Height = Math.Min(
            requestedHeight,
            DailyTimelineMetrics.TimelineHeight - Top);
    }
}