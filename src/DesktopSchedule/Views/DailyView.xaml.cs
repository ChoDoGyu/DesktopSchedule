using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using DesktopSchedule.Controls;
using DesktopSchedule.Utilities;
using DesktopSchedule.ViewModels;

namespace DesktopSchedule.Views;

/// <summary>
/// 선택 날짜의 시간 일정과 일정 목록을 표시하는 일간 View입니다.
/// 왼쪽 시간표와 오른쪽 일정 목록 모두 공통 ScheduleDragController를 사용해 클릭과 Drag 입력을 구분합니다.
/// </summary>
public partial class DailyView : UserControl
{
    private readonly ScheduleDragController _dragController;
    private DailyDragSource _dragSource = DailyDragSource.None;

    public DailyView()
    {
        InitializeComponent();

        _dragController = new ScheduleDragController(DragSurface, DragPreview, ClearDropTarget);
        DropTargetHighlight.Height = DailyTimelineMetrics.HourHeight;

        Unloaded += DailyView_Unloaded;
    }

    /// <summary>
    /// 왼쪽 시간표의 일정 카드를 눌렀을 때 클릭 또는 시간 이동 Drag 후보를 준비합니다.
    /// </summary>
    private void ScheduleButton_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement element || element.DataContext is not DailyScheduleViewModel scheduleViewModel)
        {
            return;
        }

        if (DataContext is not DailyViewModel viewModel)
        {
            return;
        }

        if (!_dragController.TryPrepareCandidate(element, scheduleViewModel.Schedule, viewModel.SelectedDate, scheduleViewModel.DisplayText, e))
        {
            return;
        }

        _dragSource = DailyDragSource.Timeline;
        e.Handled = true;
    }

    /// <summary>
    /// 오른쪽 할 일 또는 완료한 일 카드를 눌렀을 때 클릭 또는 완료 상태 변경 Drag 후보를 준비합니다.
    /// </summary>
    private void ScheduleListButton_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement element || element.DataContext is not DailyScheduleViewModel scheduleViewModel)
        {
            return;
        }

        if (DataContext is not DailyViewModel viewModel)
        {
            return;
        }

        if (!_dragController.TryPrepareCandidate(element, scheduleViewModel.Schedule, viewModel.SelectedDate, scheduleViewModel.DisplayText, e))
        {
            return;
        }

        _dragSource = scheduleViewModel.IsCompleted ? DailyDragSource.CompletedList : DailyDragSource.IncompleteList;
        e.Handled = true;
    }

    /// <summary>
    /// Drag가 시작된 영역에 따라 시간표 또는 일정 목록의 Drop 대상을 갱신합니다.
    /// </summary>
    private void DragSurface_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        var handled = _dragSource switch
        {
            DailyDragSource.Timeline => _dragController.HandleMouseMove(e, DailyTimelineArea, UpdateTimelineDropTarget),
            DailyDragSource.IncompleteList or DailyDragSource.CompletedList => _dragController.HandleMouseMove(e, DragSurface, UpdateScheduleListDropTarget),
            _ => false
        };

        if (handled)
        {
            e.Handled = true;
        }
    }

    /// <summary>
    /// 현재 마우스가 위치한 시간대를 1시간 단위 Drop 대상으로 강조합니다.
    /// </summary>
    private void UpdateTimelineDropTarget(Point timelinePosition)
    {
        ClearScheduleListDropTarget();

        var targetHour = GetTargetHour(timelinePosition);

        if (!targetHour.HasValue)
        {
            DropTargetHighlight.Visibility = Visibility.Collapsed;
            return;
        }

        var targetTop = targetHour.Value * DailyTimelineMetrics.HourHeight;

        DropTargetHighlight.Margin = new Thickness(DailyTimelineMetrics.TimeLabelWidth, targetTop, 8, 0);
        DropTargetHighlight.Visibility = Visibility.Visible;
    }

    /// <summary>
    /// 오른쪽 일정 목록 Drag 중 반대쪽 완료 상태 영역을 Drop 대상으로 강조합니다.
    /// 공통 Drop Target Resource를 사용해 다른 달력 화면과 동일한 상태 표현을 유지합니다.
    /// </summary>
    private void UpdateScheduleListDropTarget(Point dragSurfacePosition)
    {
        DropTargetHighlight.Visibility = Visibility.Collapsed;
        ClearScheduleListDropTarget();

        var target = GetScheduleListDropTarget(dragSurfacePosition);

        if (target == DailyDragSource.IncompleteList)
        {
            ApplyScheduleListDropTargetHighlight(IncompleteScheduleArea);
        }
        else if (target == DailyDragSource.CompletedList)
        {
            ApplyScheduleListDropTargetHighlight(CompletedScheduleArea);
        }
    }

    /// <summary>
    /// 지정한 일정 목록 영역을 현재 Drop 대상으로 표시합니다.
    /// 색상을 코드에 직접 지정하지 않고 AppStyles의 공통 Resource를 참조합니다.
    /// </summary>
    private static void ApplyScheduleListDropTargetHighlight(Border target)
    {
        target.SetResourceReference(Border.BackgroundProperty, "DropTargetBackgroundBrush");
        target.SetResourceReference(Border.BorderBrushProperty, "DropTargetBorderBrush");
    }

    /// <summary>
    /// 일간 타임라인의 마우스 위치를 0시부터 23시까지의 정각 시간으로 변환합니다.
    /// </summary>
    private int? GetTargetHour(Point timelinePosition)
    {
        if (DailyTimelineArea.ActualWidth <= 0 || DailyTimelineArea.ActualHeight <= 0)
        {
            return null;
        }

        var scheduleAreaRight = DailyTimelineArea.ActualWidth - 8;

        if (timelinePosition.X < DailyTimelineMetrics.TimeLabelWidth ||
            timelinePosition.X >= scheduleAreaRight ||
            timelinePosition.Y < 0 ||
            timelinePosition.Y >= DailyTimelineMetrics.TimelineHeight)
        {
            return null;
        }

        var targetHour = (int)Math.Floor(timelinePosition.Y / DailyTimelineMetrics.HourHeight);

        if (targetHour < 0 || targetHour > 23)
        {
            return null;
        }

        return targetHour;
    }

    /// <summary>
    /// 오른쪽 일정 목록 Drag의 현재 위치가 유효한 반대쪽 목록 영역에 있는지 확인합니다.
    /// </summary>
    private DailyDragSource GetScheduleListDropTarget(Point dragSurfacePosition)
    {
        if (_dragSource == DailyDragSource.IncompleteList && IsPointInside(CompletedScheduleArea, dragSurfacePosition))
        {
            return DailyDragSource.CompletedList;
        }

        if (_dragSource == DailyDragSource.CompletedList && IsPointInside(IncompleteScheduleArea, dragSurfacePosition))
        {
            return DailyDragSource.IncompleteList;
        }

        return DailyDragSource.None;
    }

    /// <summary>
    /// DragSurface 좌표의 한 점이 지정한 화면 요소 내부에 있는지 확인합니다.
    /// </summary>
    private bool IsPointInside(FrameworkElement element, Point dragSurfacePosition)
    {
        if (element.ActualWidth <= 0 || element.ActualHeight <= 0)
        {
            return false;
        }

        var topLeft = element.TranslatePoint(new Point(0, 0), DragSurface);

        return dragSurfacePosition.X >= topLeft.X &&
               dragSurfacePosition.X < topLeft.X + element.ActualWidth &&
               dragSurfacePosition.Y >= topLeft.Y &&
               dragSurfacePosition.Y < topLeft.Y + element.ActualHeight;
    }

    /// <summary>
    /// MouseUp 시 단순 클릭인지 실제 Drag였는지 확인하고 Drag가 시작된 영역에 맞는 동작을 수행합니다.
    /// </summary>
    private void DragSurface_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!_dragController.HasCandidate)
        {
            return;
        }

        var dragSource = _dragSource;
        int? targetHour = null;
        var targetList = DailyDragSource.None;

        if (_dragController.IsDragging)
        {
            if (dragSource == DailyDragSource.Timeline)
            {
                targetHour = GetTargetHour(e.GetPosition(DailyTimelineArea));
            }
            else if (dragSource is DailyDragSource.IncompleteList or DailyDragSource.CompletedList)
            {
                targetList = GetScheduleListDropTarget(e.GetPosition(DragSurface));
            }
        }

        if (!_dragController.TryCompleteRelease(out var result))
        {
            _dragSource = DailyDragSource.None;
            return;
        }

        _dragSource = DailyDragSource.None;

        if (!result.WasDragging)
        {
            if (DataContext is DailyViewModel viewModel && viewModel.SelectScheduleCommand.CanExecute(result.Schedule))
            {
                viewModel.SelectScheduleCommand.Execute(result.Schedule);
            }

            e.Handled = true;
            return;
        }

        if (dragSource == DailyDragSource.Timeline)
        {
            if (targetHour.HasValue && DataContext is DailyViewModel viewModel)
            {
                viewModel.MoveScheduleByDrop(result.Schedule, targetHour.Value);
            }

            e.Handled = true;
            return;
        }

        if (DataContext is DailyViewModel currentViewModel)
        {
            if (dragSource == DailyDragSource.IncompleteList && targetList == DailyDragSource.CompletedList)
            {
                currentViewModel.MarkScheduleAsCompletedByDrop(result.Schedule);
            }
            else if (dragSource == DailyDragSource.CompletedList && targetList == DailyDragSource.IncompleteList)
            {
                currentViewModel.MarkScheduleAsIncompleteByDrop(result.Schedule);
            }
        }

        e.Handled = true;
    }

    /// <summary>
    /// Escape를 누르면 현재 Drag를 취소합니다.
    /// </summary>
    private void DragSurface_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (!_dragController.IsDragging || e.Key != Key.Escape)
        {
            return;
        }

        _dragController.Cancel();
        _dragSource = DailyDragSource.None;

        e.Handled = true;
    }

    /// <summary>
    /// 예기치 않게 Mouse Capture가 해제되면 Drag 상태를 안전하게 정리합니다.
    /// </summary>
    private void DragSurface_LostMouseCapture(object sender, MouseEventArgs e)
    {
        if (_dragController.IsDragging && Mouse.Captured != DragSurface)
        {
            _dragController.Cancel();
            _dragSource = DailyDragSource.None;
        }
    }

    /// <summary>
    /// 일간 화면이 제거될 때 남아 있는 Drag 상태를 정리합니다.
    /// </summary>
    private void DailyView_Unloaded(object sender, RoutedEventArgs e)
    {
        _dragController.Cleanup();
        _dragSource = DailyDragSource.None;
    }

    /// <summary>
    /// 시간표와 오른쪽 일정 목록의 모든 Drop 강조를 제거합니다.
    /// </summary>
    private void ClearDropTarget()
    {
        DropTargetHighlight.Visibility = Visibility.Collapsed;
        ClearScheduleListDropTarget();
    }

    /// <summary>
    /// 할 일과 완료한 일 영역의 Drop 강조를 제거하고 기본 Border 상태로 되돌립니다.
    /// </summary>
    private void ClearScheduleListDropTarget()
    {
        ResetScheduleListDropTargetHighlight(IncompleteScheduleArea);
        ResetScheduleListDropTargetHighlight(CompletedScheduleArea);
    }

    /// <summary>
    /// Drop 대상 표시가 끝난 일정 목록 영역의 배경과 Border를 기본 상태로 복원합니다.
    /// </summary>
    private static void ResetScheduleListDropTargetHighlight(Border target)
    {
        target.Background = Brushes.Transparent;
        target.SetResourceReference(Border.BorderBrushProperty, "BorderBrush");
    }

    private enum DailyDragSource
    {
        None,
        Timeline,
        IncompleteList,
        CompletedList
    }
}