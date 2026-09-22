namespace DesktopSchedule.Utilities;

/// <summary>
/// 일간 24시간 타임라인에서 공통으로 사용하는 화면 배율 값을 제공합니다.
/// 타임라인, 일정 배치, 이후 Drag 시간 계산이 모두 같은 기준을 사용하도록 합니다.
/// </summary>
public static class DailyTimelineMetrics
{
    /// <summary>
    /// 한 시간이 화면에서 차지하는 세로 높이입니다.
    /// </summary>
    public const double HourHeight = 27.5;

    /// <summary>
    /// 24시간 전체 타임라인의 높이입니다.
    /// </summary>
    public const double TimelineHeight = HourHeight * 24.0;

    /// <summary>
    /// 시간 문자열 영역의 너비입니다.
    /// </summary>
    public const double TimeLabelWidth = 52.0;

    /// <summary>
    /// 매우 짧은 일정도 클릭할 수 있도록 보장하는 최소 카드 높이입니다.
    /// </summary>
    public const double MinimumScheduleHeight = 18.0;

    /// <summary>
    /// 지정한 분 수를 현재 타임라인 배율의 픽셀 값으로 변환합니다.
    /// </summary>
    public static double MinutesToPixels(double minutes)
    {
        return minutes / 60.0 * HourHeight;
    }
}