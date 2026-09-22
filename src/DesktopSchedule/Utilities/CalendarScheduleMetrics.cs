namespace DesktopSchedule.Utilities;

/// <summary>
/// 월간과 주간 달력의 일정 Row 배치에서 공통으로 사용하는 화면 크기 기준값을 제공합니다.
/// Panel의 실제 배치와 ViewModel의 일정 영역 높이 계산이 항상 같은 값을 사용하도록 합니다.
/// </summary>
public static class CalendarScheduleMetrics
{
    /// <summary>
    /// 일정 한 행이 차지하는 전체 세로 높이입니다.
    /// </summary>
    public const double RowHeight = 28.0;

    /// <summary>
    /// 실제 일정 카드의 높이입니다.
    /// RowHeight보다 작게 유지하여 일정 행 사이에 간격을 둡니다.
    /// </summary>
    public const double ScheduleHeight = 24.0;

    /// <summary>
    /// 일정 영역 마지막 행 아래에 확보할 여백입니다.
    /// </summary>
    public const double AreaPadding = 4.0;

    /// <summary>
    /// 날짜 셀 경계와 일정 카드 사이의 좌우 여백입니다.
    /// </summary>
    public const double HorizontalMargin = 3.0;

    /// <summary>
    /// 일정 Row 내부에서 카드 위쪽에 둘 여백입니다.
    /// </summary>
    public const double VerticalOffset = 2.0;

    /// <summary>
    /// 일정 Row 개수를 기준으로 전체 일정 표시 영역의 높이를 계산합니다.
    /// 일정이 없으면 별도 공간을 사용하지 않습니다.
    /// </summary>
    public static double GetAreaHeight(int rowCount)
    {
        if (rowCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(rowCount));
        }

        return rowCount == 0 ? 0 : rowCount * RowHeight + AreaPadding;
    }
}