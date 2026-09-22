using System.Globalization;
using System.Windows;
using System.Windows.Media;
using DesktopSchedule.Utilities;

namespace DesktopSchedule.Controls;

/// <summary>
/// 일간 화면의 00:00부터 24:00까지 시간 눈금과 구분선을 표시합니다.
/// 모든 시간대는 생략 없이 동일한 높이로 표시합니다.
/// </summary>
public class DailyTimelineGrid : FrameworkElement
{
    private static readonly Typeface TimeTypeface = new("Segoe UI");

    public DailyTimelineGrid()
    {
        Height = DailyTimelineMetrics.TimelineHeight;
        VerticalAlignment = VerticalAlignment.Top;
        SnapsToDevicePixels = true;
    }

    /// <summary>
    /// 공통 타임라인 배율에 맞는 전체 크기를 반환합니다.
    /// </summary>
    protected override Size MeasureOverride(Size availableSize)
    {
        var width = double.IsInfinity(availableSize.Width)
            ? 0
            : availableSize.Width;

        return new Size(
            width,
            DailyTimelineMetrics.TimelineHeight);
    }

    /// <summary>
    /// 00:00부터 24:00까지 한 시간 간격의 시간 문자열과
    /// 수평 구분선을 그립니다.
    /// </summary>
    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);

        var dpi = VisualTreeHelper.GetDpi(this);

        var backgroundBrush = new SolidColorBrush(
            Color.FromRgb(250, 250, 250));

        var lineBrush = new SolidColorBrush(
            Color.FromRgb(225, 225, 225));

        var timeBrush = new SolidColorBrush(
            Color.FromRgb(105, 105, 105));

        backgroundBrush.Freeze();
        lineBrush.Freeze();
        timeBrush.Freeze();

        drawingContext.DrawRectangle(
            backgroundBrush,
            null,
            new Rect(
                0,
                0,
                ActualWidth,
                DailyTimelineMetrics.TimelineHeight));

        var linePen = new Pen(lineBrush, 1.0);
        linePen.Freeze();

        for (var hour = 0; hour <= 24; hour++)
        {
            var y = hour * DailyTimelineMetrics.HourHeight;

            drawingContext.DrawLine(
                linePen,
                new Point(DailyTimelineMetrics.TimeLabelWidth, y),
                new Point(ActualWidth, y));

            var timeText = $"{hour:00}:00";

            var formattedText = new FormattedText(
                timeText,
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                TimeTypeface,
                11.0,
                timeBrush,
                dpi.PixelsPerDip);

            double textTop;

            if (hour == 0)
            {
                textTop = 2.0;
            }
            else if (hour == 24)
            {
                textTop =
                    DailyTimelineMetrics.TimelineHeight -
                    formattedText.Height -
                    2.0;
            }
            else
            {
                textTop = y - formattedText.Height / 2.0;
            }

            drawingContext.DrawText(
                formattedText,
                new Point(
                    DailyTimelineMetrics.TimeLabelWidth -
                    formattedText.Width -
                    8.0,
                    textTop));
        }

        drawingContext.DrawLine(
            linePen,
            new Point(
                DailyTimelineMetrics.TimeLabelWidth,
                0),
            new Point(
                DailyTimelineMetrics.TimeLabelWidth,
                DailyTimelineMetrics.TimelineHeight));
    }
}