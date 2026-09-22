using System.Windows;
using System.Windows.Controls;
using DesktopSchedule.Utilities;
using DesktopSchedule.ViewModels;

namespace DesktopSchedule.Controls;

/// <summary>
/// 주간과 월간 달력의 한 주 안에서 일정들을 공통 Row 구조로 배치합니다.
/// 달력의 전체 너비를 7개의 동일한 날짜 열로 나눈 뒤,
/// 일정의 시작 열, 날짜 범위, 행 번호를 기준으로 위치와 크기를 계산합니다.
/// </summary>
/// <remarks>
/// 단일 날짜 일정은 DaySpan이 1이고,
/// 여러 날짜 일정은 실제로 차지하는 날짜 수만큼 DaySpan이 증가합니다.
/// 주간과 월간 화면 모두 동일한 배치 규칙을 사용합니다.
/// </remarks>
public class CalendarSchedulePanel : Panel
{
    /// <summary>
    /// 현재 화면 너비를 기준으로 각 일정 카드가 필요로 하는 크기를 측정합니다.
    /// </summary>
    protected override Size MeasureOverride(Size availableSize)
    {
        var availableWidth = double.IsInfinity(availableSize.Width) ? 0 : availableSize.Width;
        var dayColumnWidth = availableWidth > 0 ? availableWidth / 7.0 : 0;
        var desiredHeight = 0.0;

        foreach (UIElement child in InternalChildren)
        {
            if (child is FrameworkElement element && element.DataContext is SpanningScheduleViewModelBase schedule)
            {
                var childWidth = Math.Max(0, schedule.DaySpan * dayColumnWidth - CalendarScheduleMetrics.HorizontalMargin * 2);

                child.Measure(new Size(childWidth, CalendarScheduleMetrics.ScheduleHeight));

                var scheduleBottom = schedule.RowIndex * CalendarScheduleMetrics.RowHeight + CalendarScheduleMetrics.RowHeight;
                desiredHeight = Math.Max(desiredHeight, scheduleBottom);
                continue;
            }

            child.Measure(availableSize);
        }

        return new Size(availableWidth, desiredHeight);
    }

    /// <summary>
    /// 실제 화면 너비를 7등분한 뒤
    /// 각 일정 카드를 시작 날짜 열과 날짜 범위에 맞춰 배치합니다.
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
            if (child is not FrameworkElement element || element.DataContext is not SpanningScheduleViewModelBase schedule)
            {
                child.Arrange(new Rect(new Point(), child.DesiredSize));
                continue;
            }

            var left = schedule.StartDayIndex * dayColumnWidth + CalendarScheduleMetrics.HorizontalMargin;
            var top = schedule.RowIndex * CalendarScheduleMetrics.RowHeight + CalendarScheduleMetrics.VerticalOffset;
            var width = Math.Max(0, schedule.DaySpan * dayColumnWidth - CalendarScheduleMetrics.HorizontalMargin * 2);

            child.Arrange(new Rect(left, top, width, CalendarScheduleMetrics.ScheduleHeight));
        }

        return finalSize;
    }
}