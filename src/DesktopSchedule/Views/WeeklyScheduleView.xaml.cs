using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DesktopSchedule.Controls;
using DesktopSchedule.ViewModels;

namespace DesktopSchedule.Views;

/// <summary>
/// 주간 일정 화면의 View입니다.
/// 일정 클릭과 Drag 입력을 구분하며,
/// 공통 ScheduleDragController를 사용해 Drag 상태와 화면 처리를 관리합니다.
/// 주간 달력의 Drop 대상 날짜 판정과 실제 일정 이동은 이 View가 담당합니다.
/// </summary>
public partial class WeeklyScheduleView : UserControl
{
    // 월간, 주간, 일간에서 공통으로 사용하는 일정 Drag 상태 및 동작을 관리합니다.
    private readonly ScheduleDragController _dragController;

    public WeeklyScheduleView()
    {
        InitializeComponent();

        _dragController = new ScheduleDragController(
            DragSurface,
            DragPreview,
            ClearDropTarget);

        Unloaded += WeeklyScheduleView_Unloaded;
    }

    /// <summary>
    /// 일정 카드를 눌렀을 때 공통 Drag Controller에
    /// 클릭 또는 Drag 후보 상태를 준비하도록 요청합니다.
    /// </summary>
    private void ScheduleButton_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement element)
        {
            return;
        }

        if (!_dragController.TryPrepareCandidate(
                element,
                element.DataContext,
                e))
        {
            return;
        }

        e.Handled = true;
    }

    /// <summary>
    /// 마우스 이동을 공통 Drag Controller에 전달합니다.
    /// 실제 Drag가 시작되면 Controller가 미리보기를 이동하고,
    /// 현재 마우스 위치를 이용한 Drop 대상 계산만 주간 View가 담당합니다.
    /// </summary>
    private void DragSurface_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (_dragController.HandleMouseMove(
                e,
                WeekCalendarArea,
                UpdateDropTarget))
        {
            e.Handled = true;
        }
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

        if (dayIndex < 0 ||
            dayIndex >= viewModel.Days.Count)
        {
            return null;
        }

        return viewModel.Days[dayIndex];
    }

    /// <summary>
    /// MouseUp 시 공통 Drag Controller에서
    /// 단순 클릭이었는지 실제 Drag였는지를 확인합니다.
    /// 클릭이면 일정 편집기를 열고,
    /// Drag였다면 현재 Drop 대상 날짜로 일정을 이동합니다.
    /// </summary>
    private void DragSurface_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!_dragController.HasCandidate)
        {
            return;
        }

        // 실제 Drag 중에는 Controller가 상태를 정리하기 전에
        // 현재 마우스 위치의 Drop 대상 날짜를 먼저 계산합니다.
        var targetDay = _dragController.IsDragging
            ? GetTargetDay(e.GetPosition(WeekCalendarArea))
            : null;

        if (!_dragController.TryCompleteRelease(out var result))
        {
            return;
        }

        if (!result.WasDragging)
        {
            if (DataContext is WeeklyScheduleViewModel viewModel &&
                viewModel.SelectScheduleCommand.CanExecute(result.Schedule))
            {
                viewModel.SelectScheduleCommand.Execute(result.Schedule);
                e.Handled = true;
            }

            return;
        }

        if (targetDay is not null &&
            DataContext is WeeklyScheduleViewModel currentViewModel)
        {
            currentViewModel.MoveScheduleByDrop(
                result.Schedule,
                result.DisplayDate,
                targetDay);
        }

        e.Handled = true;
    }

    /// <summary>
    /// Escape를 누르면 현재 Drag를 취소합니다.
    /// 실제 Drag 상태 정리는 공통 Controller가 담당합니다.
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
    /// 공통 Controller를 통해 Drag 상태를 정리합니다.
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
    /// 주간 화면이 제거될 때 남아 있는 Drag 관련 상태를 정리합니다.
    /// </summary>
    private void WeeklyScheduleView_Unloaded(object sender, RoutedEventArgs e)
    {
        _dragController.Cleanup();
    }

    /// <summary>
    /// 주간 화면에 표시된 모든 Drop 대상 강조를 제거합니다.
    /// 공통 Drag Controller가 Drag 종료 또는 취소 시 호출합니다.
    /// </summary>
    private void ClearDropTarget()
    {
        if (DataContext is WeeklyScheduleViewModel viewModel)
        {
            viewModel.SetDropTarget(null);
        }
    }
}