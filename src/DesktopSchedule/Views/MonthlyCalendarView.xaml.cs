using System.Windows; // Point와 FrameworkElement를 사용하기 위해 필요합니다.
using System.Windows.Controls; // UserControl과 ItemsControl을 사용하기 위해 필요합니다.
using System.Windows.Input; // Mouse와 Keyboard 입력을 처리하기 위해 필요합니다.
using DesktopSchedule.Models; // ScheduleItem을 사용하기 위해 필요합니다.
using DesktopSchedule.ViewModels; // 월간 일정 표시 ViewModel을 사용하기 위해 필요합니다.

namespace DesktopSchedule.Views;

/// <summary>
/// 월간 달력 화면의 View입니다.
/// 주간 일정 화면과 동일한 방식으로 일정 클릭과 Drag 동작을 구분하고,
/// 월간 6주 × 7일 구조에 맞춰 Drop 대상 날짜와 실제 일정 이동을 처리합니다.
/// </summary>
public partial class MonthlyCalendarView : UserControl
{
    // 일정에서 마우스를 처음 눌렀을 때 DragSurface 기준 위치입니다.
    private Point _dragStartPoint;

    // 일정 요소 내부에서 사용자가 처음 잡은 위치입니다.
    // Drag Ghost가 실제로 잡은 지점을 유지하며 따라오도록 사용합니다.
    private Point _dragPointerOffset;

    // 현재 클릭 또는 Drag 후보인 실제 일정입니다.
    private ScheduleItem? _dragSchedule;

    // 사용자가 잡은 일정 조각이 월간 달력에서 표시되고 있던 날짜입니다.
    // Drop 날짜와의 날짜 차이를 계산할 때 사용합니다.
    private DateTime _dragDisplayDate;

    // Drag Ghost에 표시할 문자열입니다.
    private string _dragDisplayText = string.Empty;

    // 현재 클릭 또는 Drag 후보가 된 화면 요소입니다.
    // 월간 화면은 일정 카드를 Border로 표시하므로 FrameworkElement로 관리합니다.
    private FrameworkElement? _dragSourceElement;

    // 마우스 이동 거리가 Drag 시작 기준을 넘었는지 여부입니다.
    private bool _isDragging;

    // Drag 시작 전 원본 일정 요소의 투명도입니다.
    // 완료 일정처럼 원래부터 투명도가 낮은 요소도 정확한 값으로 복구하기 위해 저장합니다.
    private double _dragSourceOriginalOpacity = 1.0;

    public MonthlyCalendarView()
    {
        // XAML에 정의된 UI 요소를 생성하고 연결합니다.
        InitializeComponent();

        // 화면이 제거될 때 Drag 상태가 남지 않도록 정리합니다.
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
    /// 단일 날짜 일정 카드 또는 여러 날짜 연결 막대를 눌렀을 때
    /// 주간 화면과 동일하게 공통 Drag 후보 상태를 준비합니다.
    /// </summary>
    private void ScheduleElement_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement element)
        {
            return;
        }

        if (!TryGetScheduleDragData(element.DataContext, out var schedule, out var displayDate, out var displayText))
        {
            return;
        }

        _dragStartPoint = e.GetPosition(DragSurface);
        _dragPointerOffset = e.GetPosition(element);

        _dragSchedule = schedule;
        _dragDisplayDate = displayDate;
        _dragDisplayText = displayText;
        _dragSourceElement = element;

        e.Handled = true;
    }

    /// <summary>
    /// 월간 화면의 단일 일정 카드와 여러 날짜 연결 막대에서
    /// Drag 처리에 필요한 공통 정보를 추출합니다.
    /// 주간 화면의 TryGetScheduleDragData와 같은 역할을 합니다.
    /// </summary>
    private static bool TryGetScheduleDragData(object? dataContext, out ScheduleItem schedule, out DateTime displayDate, out string displayText)
    {
        if (dataContext is MonthlyScheduleCardViewModel scheduleCard)
        {
            schedule = scheduleCard.Schedule;
            displayDate = scheduleCard.DisplayDate;
            displayText = scheduleCard.DisplayText;

            return true;
        }

        if (dataContext is MonthlySpanningScheduleViewModel spanningSchedule)
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
    /// 월간 화면에서 마우스가 움직일 때 주간 화면과 동일하게
    /// Drag 시작 거리를 검사하고 실제 Drag 상태로 전환합니다.
    /// Drag 중에는 Ghost 위치와 현재 Drop 대상 날짜를 함께 갱신합니다.
    /// </summary>
    private void DragSurface_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (_dragSchedule is null || _dragSourceElement is null)
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

        // 주간 화면과 동일한 공통 ScheduleDragPreview를
        // 현재 마우스 위치에 계속 이동시킵니다.
        DragPreview.Move(currentPosition, _dragPointerOffset);

        // 월간 달력에서 현재 마우스가 위치한 날짜를 찾아
        // 주간 화면과 동일하게 Drop 대상 날짜를 강조합니다.
        UpdateDropTarget(e.GetPosition(MonthCalendarArea));

        e.Handled = true;
    }

    /// <summary>
    /// 일정 요소를 실제 Drag 상태로 전환합니다.
    /// 주간 화면과 동일하게 Mouse Capture, 원본 투명도, Drag Ghost와 커서를 처리합니다.
    /// </summary>
    private bool BeginDrag(Point currentPosition)
    {
        if (_dragSchedule is null || _dragSourceElement is null)
        {
            return false;
        }

        if (!Mouse.Capture(DragSurface, CaptureMode.SubTree))
        {
            ClearDragCandidate();
            return false;
        }

        _isDragging = true;

        // 원본 일정은 Drag 중에도 그대로 남겨두고
        // 주간 화면과 동일하게 투명도만 낮춥니다.
        _dragSourceOriginalOpacity = _dragSourceElement.Opacity;
        _dragSourceElement.Opacity = 0.4;

        // 주간 화면에서 사용하는 동일한 ScheduleDragPreview를 재사용합니다.
        DragPreview.Show(_dragDisplayText, _dragSourceElement.ActualWidth, _dragSourceElement.ActualHeight);
        DragPreview.Move(currentPosition, _dragPointerOffset);

        Mouse.OverrideCursor = Cursors.SizeAll;

        // Escape 취소를 받을 수 있도록 DragSurface에 키보드 포커스를 둡니다.
        DragSurface.Focus();

        return true;
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

            var weekPosition = weekContainer.TranslatePoint(new Point(0, 0), MonthCalendarArea);
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
    /// MouseUp 시 주간 화면과 동일하게 단순 클릭인지 Drag인지 구분합니다.
    /// 단순 클릭이면 기존 일정 편집기를 열고,
    /// Drag였다면 현재 Drop 대상 날짜로 실제 일정을 이동합니다.
    /// </summary>
    private void DragSurface_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (_dragSchedule is null)
        {
            return;
        }

        if (!_isDragging)
        {
            var schedule = _dragSchedule;

            ClearDragCandidate();

            if (DataContext is MonthlyCalendarViewModel viewModel &&
                viewModel.SelectScheduleCommand.CanExecute(schedule))
            {
                viewModel.SelectScheduleCommand.Execute(schedule);
                e.Handled = true;
            }

            return;
        }

        // EndDragVisuals()를 호출하면 Drag 후보 정보가 초기화되므로
        // 실제 이동에 필요한 정보를 먼저 지역 변수에 보관합니다.
        var draggedSchedule = _dragSchedule;
        var draggedDisplayDate = _dragDisplayDate;
        var targetDay = GetTargetDay(e.GetPosition(MonthCalendarArea));

        // Ghost, 원본 투명도, Drop 강조, Mouse Capture를 먼저 원래 상태로 되돌립니다.
        EndDragVisuals();

        // 유효한 날짜 셀 위에서 Drop했다면
        // 주간 화면과 동일하게 ViewModel에 실제 일정 이동을 요청합니다.
        if (draggedSchedule is not null &&
            targetDay is not null &&
            DataContext is MonthlyCalendarViewModel currentViewModel)
        {
            currentViewModel.MoveScheduleByDrop(draggedSchedule, draggedDisplayDate, targetDay);
        }

        e.Handled = true;
    }

    /// <summary>
    /// Escape를 누르면 주간 화면과 동일하게 현재 Drag를 취소합니다.
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
    /// 예기치 않게 Mouse Capture가 해제되면
    /// 주간 화면과 동일하게 Drag 상태를 정리합니다.
    /// </summary>
    private void DragSurface_LostMouseCapture(object sender, MouseEventArgs e)
    {
        if (_isDragging && Mouse.Captured != DragSurface)
        {
            CancelDrag();
        }
    }

    /// <summary>
    /// 월간 화면이 제거될 때 남아 있는 Drag 상태를 정리합니다.
    /// </summary>
    private void MonthlyCalendarView_Unloaded(object sender, RoutedEventArgs e)
    {
        if (_isDragging)
        {
            EndDragVisuals();
            return;
        }

        DragPreview.Hide();

        if (DataContext is MonthlyCalendarViewModel viewModel)
        {
            viewModel.SetDropTarget(null);
        }

        Mouse.OverrideCursor = null;

        ClearDragCandidate();
    }

    /// <summary>
    /// 현재 Drag를 취소합니다.
    /// </summary>
    private void CancelDrag()
    {
        EndDragVisuals();
    }

    /// <summary>
    /// Drag와 관련된 모든 화면 상태를 원래대로 되돌립니다.
    /// 주간 화면의 EndDragVisuals와 같은 역할을 합니다.
    /// </summary>
    private void EndDragVisuals()
    {
        DragPreview.Hide();

        if (_dragSourceElement is not null)
        {
            _dragSourceElement.Opacity = _dragSourceOriginalOpacity;
        }

        // Drag가 끝나면 현재 Drop 대상 날짜 강조를 모두 제거합니다.
        if (DataContext is MonthlyCalendarViewModel viewModel)
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
        _dragSourceElement = null;
        _dragPointerOffset = default;
    }
}