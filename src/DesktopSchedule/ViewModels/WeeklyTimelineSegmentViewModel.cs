namespace DesktopSchedule.ViewModels;

/// <summary>
/// 주간 시간표에서 하나의 시간 구간을 나타냅니다.
/// 일정이 필요한 구간은 일반 높이로 표시하고,
/// 긴 빈 시간대는 작은 높이로 압축해서 표시합니다.
/// </summary>
public class WeeklyTimelineSegmentViewModel
{
    /// <summary>
    /// 구간 시작 시각을 자정 기준 분 단위로 나타냅니다.
    /// 예: 09:30은 570입니다.
    /// </summary>
    public int StartMinutes { get; }

    /// <summary>
    /// 구간 종료 시각을 자정 기준 분 단위로 나타냅니다.
    /// 24:00은 1440입니다.
    /// </summary>
    public int EndMinutes { get; }

    /// <summary>
    /// 일정이 없는 긴 시간대를 압축해서 표현하는 구간인지 여부입니다.
    /// </summary>
    public bool IsCompressed { get; }

    /// <summary>
    /// 화면에서 이 구간이 차지하는 높이입니다.
    /// </summary>
    public double Height { get; }

    /// <summary>
    /// 일반 구간 안에 표시할 시간 단위 목록입니다.
    /// 압축 구간에서는 사용하지 않습니다.
    /// </summary>
    public IReadOnlyList<int> Hours { get; }

    /// <summary>
    /// 구간 시작 시간을 표시하기 위한 문자열입니다.
    /// </summary>
    public string StartText => FormatMinutes(StartMinutes);

    /// <summary>
    /// 구간 종료 시간을 표시하기 위한 문자열입니다.
    /// </summary>
    public string EndText => FormatMinutes(EndMinutes);

    /// <summary>
    /// 압축 구간의 범위를 표시하기 위한 문자열입니다.
    /// </summary>
    public string RangeText => $"{StartText} ~ {EndText}";

    public WeeklyTimelineSegmentViewModel(int startMinutes, int endMinutes, bool isCompressed, double hourHeight, double compressedHeight)
    {
        if (startMinutes < 0 || startMinutes > 1440)
        {
            throw new ArgumentOutOfRangeException(nameof(startMinutes));
        }

        if (endMinutes < 0 || endMinutes > 1440)
        {
            throw new ArgumentOutOfRangeException(nameof(endMinutes));
        }

        if (endMinutes <= startMinutes)
        {
            throw new ArgumentException("시간 구간의 종료 시각은 시작 시각보다 늦어야 합니다.");
        }

        StartMinutes = startMinutes;
        EndMinutes = endMinutes;
        IsCompressed = isCompressed;

        if (isCompressed)
        {
            Height = compressedHeight;
            Hours = Array.Empty<int>();
            return;
        }

        Height = (EndMinutes - StartMinutes) / 60.0 * hourHeight;

        var startHour = StartMinutes / 60;
        var hourCount = (EndMinutes - StartMinutes) / 60;

        Hours = Enumerable.Range(startHour, hourCount).ToArray();
    }

    /// <summary>
    /// 자정 기준 분 값을 HH:mm 형식으로 변환합니다.
    /// </summary>
    private static string FormatMinutes(int minutes)
    {
        if (minutes == 1440)
        {
            return "24:00";
        }

        return $"{minutes / 60:00}:{minutes % 60:00}";
    }
}