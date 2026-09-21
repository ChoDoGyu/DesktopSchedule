using System.Windows;
using System.Windows.Controls;
using DesktopSchedule.ViewModels;

namespace DesktopSchedule.Controls;

/// <summary>
/// 월간 달력의 한 주 안에서 여러 날짜에 걸치는 일정 막대를 배치합니다.
/// 달력 너비를 7개의 동일한 날짜 열로 나누고,
/// 일정의 시작 열, 날짜 범위, 행 번호에 따라 자식 요소의 위치와 크기를 계산합니다.
/// </summary>
public class MonthlySpanningSchedulePanel : Panel
{
    // 여러 날짜 일정 한 행이 차지하는 세로 높이입니다.
    // MonthlyWeekViewModel의 행 높이 계산과 같은 값을 사용합니다.
    private const double RowHeight = 28.0;

    // 실제 일정 막대의 높이입니다.
    // 행 사이에 약간의 여백이 남도록 RowHeight보다 작게 사용합니다.
    private const double ScheduleHeight = 24.0;

    // 날짜 셀 경계와 일정 막대 사이의 좌우 여백입니다.
    private const double HorizontalMargin = 3.0;

    /// <summary>
    /// 자식 일정 막대들이 필요로 하는 크기를 측정합니다.
    /// 각 일정은 자신이 차지하는 날짜 수에 따라 가로 크기가 결정됩니다.
    /// </summary>
    protected override Size MeasureOverride(Size availableSize)
    {
        var availableWidth = double.IsInfinity(availableSize.Width)
            ? 0
            : availableSize.Width;

        var dayColumnWidth = availableWidth > 0
            ? availableWidth / 7.0
            : 0;

        var desiredHeight = 0.0;

        foreach (UIElement child in InternalChildren)
        {
            if (child is FrameworkElement element &&
                element.DataContext is MonthlySpanningScheduleViewModel schedule)
            {
                var childWidth = Math.Max(
                    0,
                    schedule.DaySpan * dayColumnWidth - HorizontalMargin * 2);

                child.Measure(new Size(childWidth, ScheduleHeight));

                var scheduleBottom = schedule.RowIndex * RowHeight + RowHeight;
                desiredHeight = Math.Max(desiredHeight, scheduleBottom);

                continue;
            }

            child.Measure(availableSize);
        }

        return new Size(availableWidth, desiredHeight);
    }

    /// <summary>
    /// 실제 월간 달력 너비를 7등분한 뒤
    /// 각 일정 막대를 시작 날짜 열과 날짜 범위에 맞춰 배치합니다.
    /// </summary>
    protected override Size ArrangeOverride(Size finalSize)
    {
        if (finalSize.Width <= 0)
        {
            return finalSize;
        }

        var dayColumnWidth = finalSize.Width / 7.0;

        foreach (UIElement child in InternalChildren)
        {
            if (child is not FrameworkElement element ||
                element.DataContext is not MonthlySpanningScheduleViewModel schedule)
            {
                child.Arrange(new Rect(new Point(), child.DesiredSize));
                continue;
            }

            var left = schedule.StartDayIndex * dayColumnWidth + HorizontalMargin;
            var top = schedule.RowIndex * RowHeight + 2.0;

            var width = Math.Max(
                0,
                schedule.DaySpan * dayColumnWidth - HorizontalMargin * 2);

            child.Arrange(new Rect(
                left,
                top,
                width,
                ScheduleHeight));
        }

        return finalSize;
    }
}