using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DesktopSchedule.Controls;
using DesktopSchedule.ViewModels;

namespace DesktopSchedule.Views;

/// <summary>
/// 월간 달력 화면의 View입니다.
/// 일정 클릭과 Drag 입력을 구분하며,
/// 공통 ScheduleDragController를 사용해 Drag 상태와 화면 처리를 관리합니다.
/// 월간 달력의 Drop 대상 날짜 판정과 실제 일정 이동은 이 View가 담당합니다.
/// </summary>
public partial class MonthlyCalendarView : UserControl
{
    // 월간, 주간, 일간에서 공통으로 사용하는 일정 Drag 상태 및 동작을 관리합니다.
    private readonly ScheduleDragController _dragController;

    public MonthlyCalendarView()
    {
        InitializeComponent();

        _dragController = new ScheduleDragController(
            DragSurface,
            DragPreview,
            ClearDropTarget);

        Unloaded += MonthlyCalendarView_Unloaded;
    }

    /// <summary>
    /// 사용자가 월간 달력의 빈 날짜 셀을 클릭했을 때 호출됩니다.
    /// 해당 날짜를 선택하고 새 일정 편집기를 엽니다.
    /// </summary>
    private void CalendarDay_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Left)
        {
            return;
        }

        if (sender is not FrameworkElement element ||
            element.DataContext is not MonthlyDayViewModel selectedDay)
        {
            return;
        }

        if (DataContext is not MonthlyCalendarViewModel viewModel)
        {
            return;
        }

        if (viewModel.SelectDateCommand.CanExecute(selectedDay))
        {
            viewModel.SelectDateCommand.Execute(selectedDay);
        }

        if (viewModel.OpenNewScheduleCommand.CanExecute(null))
        {
            viewModel.OpenNewScheduleCommand.Execute(null);
        }

        e.Handled = true;
    }

    /// <summary>
    /// 일정 카드를 눌렀을 때 공통 Drag Controller에
    /// 클릭 또는 Drag 후보 상태를 준비하도록 요청합니다.
    /// </summary>
    private void ScheduleElement_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
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
    /// 현재 마우스 위치를 이용한 Drop 대상 계산만 월간 View가 담당합니다.
    /// </summary>
    private void DragSurface_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (_dragController.HandleMouseMove(
                e,
                MonthCalendarArea,
                UpdateDropTarget))
        {
            e.Handled = true;
        }
    }

    /// <summary>
    /// 현재 마우스 위치를 기준으로 Drop 대상 날짜를 계산하고
    /// ViewModel에 전달하여 해당 날짜 하나만 강조합니다.
    /// </summary>
    private void UpdateDropTarget(Point calendarPosition)
    {
        if (DataContext is not MonthlyCalendarViewModel viewModel)
        {
            return;
        }

        viewModel.SetDropTarget(GetTargetDay(calendarPosition));
    }

    /// <summary>
    /// 월간 6주 × 7일 영역에서 현재 마우스가 가리키는 날짜를 찾습니다.
    /// X 위치로 요일 열을 결정하고,
    /// 실제 렌더링된 각 주 컨테이너의 Y 위치와 높이를 이용해 주를 결정합니다.
    /// </summary>
    private MonthlyDayViewModel? GetTargetDay(Point calendarPosition)
    {
        if (DataContext is not MonthlyCalendarViewModel viewModel)
        {
            return null;
        }

        if (MonthCalendarArea.ActualWidth <= 0 ||
            MonthCalendarArea.ActualHeight <= 0)
        {
            return null;
        }

        if (calendarPosition.X < 0 ||
            calendarPosition.X >= MonthCalendarArea.ActualWidth ||
            calendarPosition.Y < 0 ||
            calendarPosition.Y >= MonthCalendarArea.ActualHeight)
        {
            return null;
        }

        // 월간 달력의 모든 주는 일요일부터 토요일까지 동일한 7열 구조이므로
        // X 위치를 7등분하여 현재 요일 열 번호를 구합니다.
        var dayWidth = MonthCalendarArea.ActualWidth / 7.0;
        var dayIndex = (int)(calendarPosition.X / dayWidth);

        if (dayIndex < 0 || dayIndex >= 7)
        {
            return null;
        }

        // 월간 달력은 일정 개수에 따라 각 주의 실제 높이가 달라질 수 있으므로
        // 전체 높이를 단순히 6등분하지 않고 실제 렌더링된 주의 범위를 확인합니다.
        for (var weekIndex = 0; weekIndex < viewModel.Weeks.Count; weekIndex++)
        {
            if (MonthCalendarArea.ItemContainerGenerator.ContainerFromIndex(weekIndex) is not FrameworkElement weekContainer)
            {
                continue;
            }

            var weekPosition = weekContainer.TranslatePoint(
                new Point(0, 0),
                MonthCalendarArea);

            var weekTop = weekPosition.Y;
            var weekBottom = weekTop + weekContainer.ActualHeight;

            if (calendarPosition.Y < weekTop ||
                calendarPosition.Y >= weekBottom)
            {
                continue;
            }

            var week = viewModel.Weeks[weekIndex];

            if (dayIndex >= week.Days.Count)
            {
                return null;
            }

            return week.Days[dayIndex];
        }

        return null;
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
            ? GetTargetDay(e.GetPosition(MonthCalendarArea))
            : null;

        if (!_dragController.TryCompleteRelease(out var result))
        {
            return;
        }

        if (!result.WasDragging)
        {
            if (DataContext is MonthlyCalendarViewModel viewModel &&
                viewModel.SelectScheduleCommand.CanExecute(result.Schedule))
            {
                viewModel.SelectScheduleCommand.Execute(result.Schedule);
                e.Handled = true;
            }

            return;
        }

        if (targetDay is not null &&
            DataContext is MonthlyCalendarViewModel currentViewModel)
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
    /// 월간 화면이 제거될 때 남아 있는 Drag 관련 상태를 정리합니다.
    /// </summary>
    private void MonthlyCalendarView_Unloaded(object sender, RoutedEventArgs e)
    {
        _dragController.Cleanup();
    }

    /// <summary>
    /// 월간 화면에 표시된 모든 Drop 대상 강조를 제거합니다.
    /// 공통 Drag Controller가 Drag 종료 또는 취소 시 호출합니다.
    /// </summary>
    private void ClearDropTarget()
    {
        if (DataContext is MonthlyCalendarViewModel viewModel)
        {
            viewModel.SetDropTarget(null);
        }
    }
}