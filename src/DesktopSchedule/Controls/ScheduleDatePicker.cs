using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;

namespace DesktopSchedule.Controls;

/// <summary>
/// DesktopSchedule에서 공통으로 사용하는 날짜 선택 컨트롤입니다.
/// 기본 DatePicker의 달력 팝업을 컨트롤의 오른쪽 끝에 맞춰 표시합니다.
/// </summary>
public class ScheduleDatePicker : DatePicker
{
    public ScheduleDatePicker()
    {
        CalendarOpened += OnCalendarOpened;
    }

    /// <summary>
    /// 달력이 열린 뒤 실제 달력 너비를 확인하여 DatePicker의 오른쪽 끝에 맞춥니다.
    /// </summary>
    private void OnCalendarOpened(object? sender, RoutedEventArgs e)
    {
        Dispatcher.BeginInvoke(AlignCalendarPopup, DispatcherPriority.Loaded);
    }

    /// <summary>
    /// DatePicker 전체의 오른쪽 끝과 달력 팝업의 오른쪽 끝을 맞춥니다.
    /// </summary>
    private void AlignCalendarPopup()
    {
        ApplyTemplate();

        if (Template.FindName("PART_Popup", this) is not Popup popup)
        {
            return;
        }

        popup.PlacementTarget = this;
        popup.Placement = PlacementMode.Bottom;
        popup.VerticalOffset = 2;

        if (popup.Child is not FrameworkElement popupContent)
        {
            return;
        }

        if (popupContent.ActualWidth <= 0)
        {
            popupContent.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        }

        var popupWidth = popupContent.ActualWidth > 0 ? popupContent.ActualWidth : popupContent.DesiredSize.Width;

        if (popupWidth <= 0)
        {
            return;
        }

        popup.HorizontalOffset = ActualWidth - popupWidth;
    }
}