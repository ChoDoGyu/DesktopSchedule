using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DesktopSchedule.Models;
using DesktopSchedule.ViewModels;

namespace DesktopSchedule.Views;

/// <summary>
/// 주간 일정 화면의 View입니다.
/// 일정 카드의 클릭과 내부 Drag 동작을 구분하여 처리합니다.
/// </summary>
public partial class WeeklyScheduleView : UserControl
{
    private Point _dragStartPoint;
    private Point _dragPointerOffset;

    private ScheduleItem? _dragSchedule;
    private DateTime _dragDisplayDate;
    private string _dragDisplayText = string.Empty;

    private Button? _dragSourceButton;

    private bool _isDragging;

    private double _dragSourceOriginalOpacity = 1.0;

    public WeeklyScheduleView()
    {
        InitializeComponent();

        Unloaded += WeeklyScheduleView_Unloaded;
    }

    /// <summary>
    /// 단일 날짜 일정 카드 또는 여러 날짜 연결 막대를 클릭했을 때
    /// 공통 Drag 후보 상태를 준비합니다.
    /// </summary>
    private void ScheduleButton_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not Button button)
        {
            return;
        }

        if (!TryGetScheduleDragData(
                button.DataContext,
                out var schedule,
                out var displayDate,
                out var displayText))
        {
            return;
        }

        _dragStartPoint = e.GetPosition(DragSurface);
        _dragPointerOffset = e.GetPosition(button);

        _dragSchedule = schedule;
        _dragDisplayDate = displayDate;
        _dragDisplayText = displayText;
        _dragSourceButton = button;

        e.Handled = true;
    }

    /// <summary>
    /// 화면용 ViewModel에서 Drag에 필요한 공통 일정 정보를 추출합니다.
    /// </summary>
    private static bool TryGetScheduleDragData(object? dataContext, out ScheduleItem schedule, out DateTime displayDate, out string displayText)
    {
        if (dataContext is WeeklyScheduleCardViewModel scheduleCard)
        {
            schedule = scheduleCard.Schedule;
            displayDate = scheduleCard.DisplayDate;
            displayText = scheduleCard.DisplayText;

            return true;
        }

        if (dataContext is WeeklySpanningScheduleViewModel spanningSchedule)
        {
            schedule = spanningSchedule.Schedule;
            displayDate = spanningSchedule.DisplayDate;
            displayText = spanningSchedule.DisplayText;

            return true;
        }

        schedule = null!;
        displayDate = default;
        displayText = string.Empty;

        return false;
    }

    /// <summary>
    /// 주간 화면에서 마우스가 움직일 때 Drag 시작 여부를 판정하고
    /// 실제 Drag 중이면 미리보기와 대상 날짜를 갱신합니다.
    /// </summary>
    private void DragSurface_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (_dragSchedule is null || _dragSourceButton is null)
        {
            return;
        }

        if (e.LeftButton != MouseButtonState.Pressed)
        {
            if (_isDragging)
            {
                CancelDrag();
            }
            else
            {
                ClearDragCandidate();
            }

            return;
        }

        var currentPosition = e.GetPosition(DragSurface);

        if (!_isDragging)
        {
            var horizontalDistance = Math.Abs(currentPosition.X - _dragStartPoint.X);
            var verticalDistance = Math.Abs(currentPosition.Y - _dragStartPoint.Y);

            if (horizontalDistance < SystemParameters.MinimumHorizontalDragDistance &&
                verticalDistance < SystemParameters.MinimumVerticalDragDistance)
            {
                return;
            }

            if (!BeginDrag(currentPosition))
            {
                return;
            }
        }

        DragPreview.Move(currentPosition, _dragPointerOffset);

        UpdateDropTarget(e.GetPosition(WeekCalendarArea));

        e.Handled = true;
    }

    /// <summary>
    /// 일정 카드를 실제 Drag 상태로 전환합니다.
    /// </summary>
    private bool BeginDrag(Point currentPosition)
    {
        if (_dragSchedule is null || _dragSourceButton is null)
        {
            return false;
        }

        if (!Mouse.Capture(DragSurface, CaptureMode.SubTree))
        {
            ClearDragCandidate();
            return false;
        }

        _isDragging = true;

        _dragSourceOriginalOpacity = _dragSourceButton.Opacity;
        _dragSourceButton.Opacity = 0.4;

        DragPreview.Show(
            _dragDisplayText,
            _dragSourceButton.ActualWidth,
            _dragSourceButton.ActualHeight);

        DragPreview.Move(currentPosition, _dragPointerOffset);

        Mouse.OverrideCursor = Cursors.SizeAll;

        return true;
    }

    /// <summary>
    /// 현재 마우스 위치의 날짜를 Drop 대상으로 강조합니다.
    /// </summary>
    private void UpdateDropTarget(Point calendarPosition)
    {
        if (DataContext is not WeeklyScheduleViewModel viewModel)
        {
            return;
        }

        viewModel.SetDropTarget(GetTargetDay(calendarPosition));
    }

    /// <summary>
    /// 주간 7일 영역의 X 위치를 이용해 현재 대상 날짜를 계산합니다.
    /// </summary>
    private WeeklyDayViewModel? GetTargetDay(Point calendarPosition)
    {
        if (DataContext is not WeeklyScheduleViewModel viewModel)
        {
            return null;
        }

        if (WeekCalendarArea.ActualWidth <= 0 ||
            WeekCalendarArea.ActualHeight <= 0)
        {
            return null;
        }

        if (calendarPosition.X < 0 ||
            calendarPosition.X >= WeekCalendarArea.ActualWidth ||
            calendarPosition.Y < 0 ||
            calendarPosition.Y >= WeekCalendarArea.ActualHeight)
        {
            return null;
        }

        var dayWidth = WeekCalendarArea.ActualWidth / 7.0;
        var dayIndex = (int)(calendarPosition.X / dayWidth);

        if (dayIndex < 0 || dayIndex >= viewModel.Days.Count)
        {
            return null;
        }

        return viewModel.Days[dayIndex];
    }

    /// <summary>
    /// MouseUp 시 단순 클릭인지 Drag인지 구분합니다.
    /// </summary>
    private void DragSurface_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!_isDragging)
        {
            var schedule = _dragSchedule;

            ClearDragCandidate();

            if (schedule is not null &&
                DataContext is WeeklyScheduleViewModel viewModel)
            {
                viewModel.SelectScheduleCommand.Execute(schedule);
                e.Handled = true;
            }

            return;
        }

        var draggedSchedule = _dragSchedule;
        var draggedDisplayDate = _dragDisplayDate;
        var targetDay = GetTargetDay(e.GetPosition(WeekCalendarArea));

        EndDragVisuals();

        if (draggedSchedule is not null &&
            targetDay is not null &&
            DataContext is WeeklyScheduleViewModel currentViewModel)
        {
            currentViewModel.MoveScheduleByDrop(
                draggedSchedule,
                draggedDisplayDate,
                targetDay);
        }

        e.Handled = true;
    }

    /// <summary>
    /// Escape를 누르면 Drag를 취소합니다.
    /// </summary>
    private void DragSurface_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (!_isDragging || e.Key != Key.Escape)
        {
            return;
        }

        CancelDrag();

        e.Handled = true;
    }

    /// <summary>
    /// 예기치 않게 Mouse Capture가 해제되면 Drag 상태를 정리합니다.
    /// </summary>
    private void DragSurface_LostMouseCapture(object sender, MouseEventArgs e)
    {
        if (_isDragging && Mouse.Captured != DragSurface)
        {
            CancelDrag();
        }
    }

    /// <summary>
    /// 화면이 제거될 때 남아 있는 Drag 상태를 정리합니다.
    /// </summary>
    private void WeeklyScheduleView_Unloaded(object sender, RoutedEventArgs e)
    {
        if (_isDragging)
        {
            EndDragVisuals();
            return;
        }

        DragPreview.Hide();

        if (DataContext is WeeklyScheduleViewModel viewModel)
        {
            viewModel.SetDropTarget(null);
        }

        ClearDragCandidate();
    }

    private void CancelDrag()
    {
        EndDragVisuals();
    }

    /// <summary>
    /// Drag와 관련된 모든 화면 상태를 원래대로 되돌립니다.
    /// </summary>
    private void EndDragVisuals()
    {
        DragPreview.Hide();

        if (_dragSourceButton is not null)
        {
            _dragSourceButton.Opacity = _dragSourceOriginalOpacity;
            VisualStateManager.GoToState(_dragSourceButton, "Normal", false);
        }

        if (DataContext is WeeklyScheduleViewModel viewModel)
        {
            viewModel.SetDropTarget(null);
        }

        Mouse.OverrideCursor = null;

        _isDragging = false;

        if (Mouse.Captured == DragSurface)
        {
            Mouse.Capture(null);
        }

        ClearDragCandidate();
    }

    /// <summary>
    /// 클릭 또는 Drag 후보 정보를 초기화합니다.
    /// </summary>
    private void ClearDragCandidate()
    {
        _dragSchedule = null;
        _dragDisplayDate = default;
        _dragDisplayText = string.Empty;
        _dragSourceButton = null;
    }
}