using System.Windows;
using System.Windows.Controls;
using DesktopSchedule.Utilities;
using DesktopSchedule.ViewModels;

namespace DesktopSchedule.Controls;

/// <summary>
/// 일간 타임라인 위에 일정 카드를 실제 시간 위치에 맞춰 배치합니다.
/// 시간이 겹치는 일정은 같은 가로 영역을 나누어 동시에 볼 수 있도록 배치합니다.
/// </summary>
public class DailySchedulePanel : Panel
{
    // 일정 영역의 좌우 바깥 여백입니다.
    private const double HorizontalMargin = 4.0;

    // 동시에 표시되는 일정 카드 사이의 가로 간격입니다.
    private const double ColumnGap = 3.0;

    /// <summary>
    /// 각 일정 카드의 겹침 열 개수를 기준으로 필요한 가로 크기를 측정합니다.
    /// </summary>
    protected override Size MeasureOverride(Size availableSize)
    {
        var availableWidth = double.IsInfinity(availableSize.Width)
            ? 0
            : availableSize.Width;

        foreach (UIElement child in InternalChildren)
        {
            if (child is FrameworkElement element &&
                element.DataContext is DailyScheduleViewModel schedule)
            {
                var childWidth = CalculateColumnWidth(
                    availableWidth,
                    schedule.ColumnCount);

                child.Measure(
                    new Size(
                        childWidth,
                        schedule.Height));

                continue;
            }

            child.Measure(availableSize);
        }

        return new Size(
            availableWidth,
            DailyTimelineMetrics.TimelineHeight);
    }

    /// <summary>
    /// 일정의 실제 시간 위치와 겹침 열 번호를 기준으로
    /// 세로 위치와 가로 위치를 함께 결정합니다.
    /// </summary>
    protected override Size ArrangeOverride(Size finalSize)
    {
        foreach (UIElement child in InternalChildren)
        {
            if (child is not FrameworkElement element ||
                element.DataContext is not DailyScheduleViewModel schedule)
            {
                child.Arrange(
                    new Rect(
                        new Point(),
                        child.DesiredSize));

                continue;
            }

            var columnWidth = CalculateColumnWidth(
                finalSize.Width,
                schedule.ColumnCount);

            var left =
                HorizontalMargin +
                schedule.ColumnIndex * (columnWidth + ColumnGap);

            child.Arrange(
                new Rect(
                    left,
                    schedule.Top,
                    columnWidth,
                    schedule.Height));
        }

        return finalSize;
    }

    /// <summary>
    /// 전체 일정 영역을 지정된 열 개수로 나누어
    /// 일정 카드 하나가 사용할 가로 너비를 계산합니다.
    /// </summary>
    private static double CalculateColumnWidth(double totalWidth, int columnCount)
    {
        var contentWidth = Math.Max(
            0,
            totalWidth -
            HorizontalMargin * 2 -
            ColumnGap * (columnCount - 1));

        return columnCount <= 0
            ? 0
            : contentWidth / columnCount;
    }
}