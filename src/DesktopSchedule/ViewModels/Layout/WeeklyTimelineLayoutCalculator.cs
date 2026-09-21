using DesktopSchedule.Models;

namespace DesktopSchedule.ViewModels.Layout;

/// <summary>
/// 주간 화면에서 사용하는 공통 압축 시간축을 계산합니다.
/// 일정이 있는 시간대는 일반 높이로 유지하고 긴 빈 시간대는 압축합니다.
/// </summary>
public class WeeklyTimelineLayoutCalculator
{
    private const int ActivePaddingMinutes = 60;
    private const int MinimumCompressibleGapMinutes = 120;
    private const double CompressedSegmentHeight = 24.0;

    /// <summary>
    /// 일반 시간대에서 한 시간을 표현할 높이입니다.
    /// </summary>
    public double HourHeight => 25.0;

    /// <summary>
    /// 현재 주의 모든 시간 일정을 기준으로 하나의 공통 압축 시간축을 생성합니다.
    /// </summary>
    public IReadOnlyList<WeeklyTimelineSegmentViewModel> CreateSegments(IReadOnlyList<ScheduleItem> schedules, DateTime weekStartDate)
    {
        var activeRanges = CreateActiveRanges(schedules, weekStartDate);

        if (activeRanges.Count == 0)
        {
            return new[]
            {
                CreateSegment(0, 1440, true)
            };
        }

        var mergedRanges = MergeRanges(activeRanges);
        var segments = new List<WeeklyTimelineSegmentViewModel>();
        var currentMinutes = 0;

        foreach (var range in mergedRanges)
        {
            AddGapSegment(segments, currentMinutes, range.StartMinutes);
            segments.Add(CreateSegment(range.StartMinutes, range.EndMinutes, false));
            currentMinutes = range.EndMinutes;
        }

        AddGapSegment(segments, currentMinutes, 1440);

        return segments;
    }

    /// <summary>
    /// 현재 압축 시간축 전체의 화면 높이를 계산합니다.
    /// </summary>
    public double GetDisplayHeight(IEnumerable<WeeklyTimelineSegmentViewModel> segments)
    {
        return segments.Sum(segment => segment.Height);
    }

    /// <summary>
    /// 자정 기준 분 값을 현재 압축 시간축의 Y 위치로 변환합니다.
    /// </summary>
    public double GetTimelineOffset(IReadOnlyList<WeeklyTimelineSegmentViewModel> segments, int minutes)
    {
        var offset = 0.0;

        foreach (var segment in segments)
        {
            if (minutes >= segment.EndMinutes)
            {
                offset += segment.Height;
                continue;
            }

            if (minutes <= segment.StartMinutes)
            {
                return offset;
            }

            var minutesInsideSegment = minutes - segment.StartMinutes;

            if (segment.IsCompressed)
            {
                var segmentDuration = segment.EndMinutes - segment.StartMinutes;
                var progress = minutesInsideSegment / (double)segmentDuration;

                return offset + segment.Height * progress;
            }

            return offset + minutesInsideSegment / 60.0 * HourHeight;
        }

        return offset;
    }

    /// <summary>
    /// 압축 시간축의 Y 위치를 다시 자정 기준 분 값으로 변환합니다.
    /// Drag & Drop에서 마우스 위치를 실제 시간으로 계산할 때 사용합니다.
    /// </summary>
    public int GetMinutesFromTimelineOffset(IReadOnlyList<WeeklyTimelineSegmentViewModel> segments, double offset)
    {
        if (segments.Count == 0)
        {
            return 0;
        }

        var displayHeight = GetDisplayHeight(segments);
        offset = Math.Clamp(offset, 0, displayHeight);

        var currentOffset = 0.0;

        foreach (var segment in segments)
        {
            var segmentEndOffset = currentOffset + segment.Height;

            if (offset > segmentEndOffset)
            {
                currentOffset = segmentEndOffset;
                continue;
            }

            if (segment.Height <= 0)
            {
                return segment.StartMinutes;
            }

            var progress = (offset - currentOffset) / segment.Height;
            progress = Math.Clamp(progress, 0, 1);

            var durationMinutes = segment.EndMinutes - segment.StartMinutes;
            var minutes = segment.StartMinutes + durationMinutes * progress;

            return (int)Math.Round(minutes, MidpointRounding.AwayFromZero);
        }

        return 1440;
    }

    /// <summary>
    /// 지정한 일정이 특정 날짜에 실제로 차지하는 시간 범위를 계산합니다.
    /// 여러 날짜 일정이면 현재 날짜에 해당하는 부분만 잘라냅니다.
    /// </summary>
    public bool TryGetTimedSegmentMinutes(ScheduleItem schedule, DateTime date, out int startMinutes, out int endMinutes)
    {
        startMinutes = 0;
        endMinutes = 0;

        if (schedule.IsAllDay)
        {
            return false;
        }

        var dayStart = date.Date;
        var dayEnd = dayStart.AddDays(1);

        if (schedule.EndAt <= dayStart || schedule.StartAt >= dayEnd)
        {
            return false;
        }

        var segmentStart = schedule.StartAt < dayStart ? dayStart : schedule.StartAt;
        var segmentEnd = schedule.EndAt > dayEnd ? dayEnd : schedule.EndAt;

        startMinutes = (int)(segmentStart - dayStart).TotalMinutes;
        endMinutes = segmentEnd >= dayEnd ? 1440 : (int)(segmentEnd - dayStart).TotalMinutes;

        return endMinutes > startMinutes;
    }

    /// <summary>
    /// 현재 주에 표시되는 모든 시간 일정 조각을 기준으로 활성 시간 범위를 만듭니다.
    /// </summary>
    private List<TimeRange> CreateActiveRanges(IReadOnlyList<ScheduleItem> schedules, DateTime weekStartDate)
    {
        var ranges = new List<TimeRange>();

        for (var dayOffset = 0; dayOffset < 7; dayOffset++)
        {
            var date = weekStartDate.Date.AddDays(dayOffset);

            foreach (var schedule in schedules)
            {
                if (!TryGetTimedSegmentMinutes(schedule, date, out var startMinutes, out var endMinutes))
                {
                    continue;
                }

                var paddedStart = Math.Max(0, FloorToHour(startMinutes) - ActivePaddingMinutes);
                var paddedEnd = Math.Min(1440, CeilingToHour(endMinutes) + ActivePaddingMinutes);

                ranges.Add(new TimeRange(paddedStart, paddedEnd));
            }
        }

        return ranges;
    }

    /// <summary>
    /// 서로 겹치거나 맞닿아 있는 활성 시간 범위를 하나로 합칩니다.
    /// </summary>
    private static List<TimeRange> MergeRanges(List<TimeRange> ranges)
    {
        if (ranges.Count == 0)
        {
            return new List<TimeRange>();
        }

        var orderedRanges = ranges.OrderBy(range => range.StartMinutes).ThenBy(range => range.EndMinutes).ToList();
        var mergedRanges = new List<TimeRange>();
        var currentRange = orderedRanges[0];

        for (var index = 1; index < orderedRanges.Count; index++)
        {
            var nextRange = orderedRanges[index];

            if (nextRange.StartMinutes <= currentRange.EndMinutes)
            {
                currentRange = new TimeRange(currentRange.StartMinutes, Math.Max(currentRange.EndMinutes, nextRange.EndMinutes));
                continue;
            }

            mergedRanges.Add(currentRange);
            currentRange = nextRange;
        }

        mergedRanges.Add(currentRange);

        return mergedRanges;
    }

    /// <summary>
    /// 활성 구간 사이의 빈 시간을 시간축에 추가합니다.
    /// </summary>
    private void AddGapSegment(List<WeeklyTimelineSegmentViewModel> segments, int startMinutes, int endMinutes)
    {
        if (endMinutes <= startMinutes)
        {
            return;
        }

        var durationMinutes = endMinutes - startMinutes;
        var isCompressed = durationMinutes >= MinimumCompressibleGapMinutes;

        segments.Add(CreateSegment(startMinutes, endMinutes, isCompressed));
    }

    /// <summary>
    /// 시간축에 사용할 하나의 구간을 생성합니다.
    /// </summary>
    private WeeklyTimelineSegmentViewModel CreateSegment(int startMinutes, int endMinutes, bool isCompressed)
    {
        return new WeeklyTimelineSegmentViewModel(startMinutes, endMinutes, isCompressed, HourHeight, CompressedSegmentHeight);
    }

    private static int FloorToHour(int minutes)
    {
        return minutes / 60 * 60;
    }

    private static int CeilingToHour(int minutes)
    {
        return (int)Math.Ceiling(minutes / 60.0) * 60;
    }

    private readonly record struct TimeRange(int StartMinutes, int EndMinutes);
}