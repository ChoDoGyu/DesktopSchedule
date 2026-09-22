using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DesktopSchedule.Controls;
using DesktopSchedule.Utilities;
using DesktopSchedule.ViewModels;

namespace DesktopSchedule.Views;

/// <summary>
/// 선택 날짜의 시간 일정과 일정 목록을 표시하는 일간 View입니다.
/// 일정 클릭과 Drag 입력을 구분하며,
/// 공통 ScheduleDragController를 사용해 Drag 상태를 관리합니다.
/// </summary>
public partial class DailyView : UserControl
{
    // 월간과 주간에서도 사용하는 공통 일정 Drag Controller입니다.
    private readonly ScheduleDragController _dragController;

    public DailyView()
    {
        InitializeComponent();

        _dragController = new ScheduleDragController(
            DragSurface,
            DragPreview,
            ClearDropTarget);

        // Drop 대상 강조 한 칸의 높이는
        // 실제 일간 타임라인의 한 시간 높이와 정확히 일치시킵니다.
        DropTargetHighlight.Height =
            DailyTimelineMetrics.HourHeight;

        Unloaded += DailyView_Unloaded;
    }

    /// <summary>
    /// 왼쪽 시간표의 일정 카드를 눌렀을 때
    /// 클릭 또는 Drag 후보 상태를 준비합니다.
    /// </summary>
    private void ScheduleButton_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement element ||
            element.DataContext is not DailyScheduleViewModel scheduleViewModel)
        {
            return;
        }

        if (DataContext is not DailyViewModel viewModel)
        {
            return;
        }

        if (!_dragController.TryPrepareCandidate(
                element,
                scheduleViewModel.Schedule,
                viewModel.SelectedDate,
                scheduleViewModel.DisplayText,
                e))
        {
            return;
        }

        e.Handled = true;
    }

    /// <summary>
    /// 마우스 이동을 공통 Drag Controller에 전달합니다.
    /// 실제 Drag가 시작되면 현재 마우스 위치의 시간대를 계산하여 강조합니다.
    /// </summary>
    private void DragSurface_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (_dragController.HandleMouseMove(
                e,
                DailyTimelineArea,
                UpdateDropTarget))
        {
            e.Handled = true;
        }
    }

    /// <summary>
    /// 현재 마우스가 위치한 시간대를
    /// 1시간 단위 Drop 대상으로 강조합니다.
    /// </summary>
    private void UpdateDropTarget(Point timelinePosition)
    {
        var targetHour =
            GetTargetHour(timelinePosition);

        if (!targetHour.HasValue)
        {
            ClearDropTarget();
            return;
        }

        var targetTop =
            targetHour.Value *
            DailyTimelineMetrics.HourHeight;

        DropTargetHighlight.Margin =
            new Thickness(
                DailyTimelineMetrics.TimeLabelWidth,
                targetTop,
                8,
                0);

        DropTargetHighlight.Visibility =
            Visibility.Visible;
    }

    /// <summary>
    /// 일간 타임라인의 마우스 위치를
    /// 0시부터 23시까지의 정각 시간으로 변환합니다.
    /// 시간 표시 영역 밖이나 타임라인 밖이면 null을 반환합니다.
    /// </summary>
    private int? GetTargetHour(Point timelinePosition)
    {
        if (DailyTimelineArea.ActualWidth <= 0 ||
            DailyTimelineArea.ActualHeight <= 0)
        {
            return null;
        }

        var scheduleAreaRight =
            DailyTimelineArea.ActualWidth - 8;

        if (timelinePosition.X < DailyTimelineMetrics.TimeLabelWidth ||
            timelinePosition.X >= scheduleAreaRight ||
            timelinePosition.Y < 0 ||
            timelinePosition.Y >= DailyTimelineMetrics.TimelineHeight)
        {
            return null;
        }

        var targetHour =
            (int)Math.Floor(
                timelinePosition.Y /
                DailyTimelineMetrics.HourHeight);

        if (targetHour < 0 ||
            targetHour > 23)
        {
            return null;
        }

        return targetHour;
    }

    /// <summary>
    /// MouseUp 시 단순 클릭인지 실제 Drag였는지 확인합니다.
    /// 클릭이면 기존 일정 수정 화면을 열고,
    /// Drag이면 선택 날짜의 대상 정각으로 일정을 이동합니다.
    /// </summary>
    private void DragSurface_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!_dragController.HasCandidate)
        {
            return;
        }

        // Controller가 Drag 상태를 정리하면 Drop 강조도 제거되므로
        // 먼저 현재 마우스 위치의 대상 시간을 계산합니다.
        var targetHour = _dragController.IsDragging
            ? GetTargetHour(
                e.GetPosition(DailyTimelineArea))
            : null;

        if (!_dragController.TryCompleteRelease(
                out var result))
        {
            return;
        }

        // Drag 거리 기준을 넘지 않았다면
        // 기존과 동일하게 일정 수정 화면을 엽니다.
        if (!result.WasDragging)
        {
            if (DataContext is DailyViewModel viewModel &&
                viewModel.SelectScheduleCommand.CanExecute(
                    result.Schedule))
            {
                viewModel.SelectScheduleCommand.Execute(
                    result.Schedule);

                e.Handled = true;
            }

            return;
        }

        // 실제 Drag였지만 타임라인의 유효한 시간 영역 밖에 놓았다면
        // 아무 일정 변경 없이 Drag만 종료합니다.
        if (!targetHour.HasValue)
        {
            e.Handled = true;
            return;
        }

        if (DataContext is DailyViewModel currentViewModel)
        {
            currentViewModel.MoveScheduleByDrop(
                result.Schedule,
                targetHour.Value);
        }

        e.Handled = true;
    }

    /// <summary>
    /// Escape를 누르면 현재 Drag를 취소합니다.
    /// </summary>
    private void DragSurface_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (!_dragController.IsDragging ||
            e.Key != Key.Escape)
        {
            return;
        }

        _dragController.Cancel();

        e.Handled = true;
    }

    /// <summary>
    /// 예기치 않게 Mouse Capture가 해제되면
    /// Drag 상태를 안전하게 정리합니다.
    /// </summary>
    private void DragSurface_LostMouseCapture(object sender, MouseEventArgs e)
    {
        if (_dragController.IsDragging &&
            Mouse.Captured != DragSurface)
        {
            _dragController.Cancel();
        }
    }

    /// <summary>
    /// 일간 화면이 제거될 때 남아 있는 Drag 상태를 정리합니다.
    /// </summary>
    private void DailyView_Unloaded(object sender, RoutedEventArgs e)
    {
        _dragController.Cleanup();
    }

    /// <summary>
    /// 현재 표시 중인 시간 Drop 대상 강조를 제거합니다.
    /// </summary>
    private void ClearDropTarget()
    {
        DropTargetHighlight.Visibility =
            Visibility.Collapsed;
    }
}