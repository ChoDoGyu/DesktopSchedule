using System.Windows;
using System.Windows.Input;
using DesktopSchedule.Models;
using DesktopSchedule.ViewModels;

namespace DesktopSchedule.Controls;

/// <summary>
/// 주간과 월간 일정 화면에서 공통으로 사용하는 Drag 상태와 동작을 관리합니다.
/// Drag 후보 준비, 시작 거리 판정, Mouse Capture, 미리보기 이동,
/// 원본 요소 표시 상태 복원 및 Drag 종료 처리를 담당합니다.
/// </summary>
/// <remarks>
/// 실제 Drop 대상 날짜를 찾거나 일정을 이동하는 작업은
/// 각 화면의 달력 구조가 다르므로 해당 View가 담당합니다.
/// </remarks>
public sealed class ScheduleDragController
{
    private readonly FrameworkElement _dragSurface;
    private readonly ScheduleDragPreview _dragPreview;
    private readonly Action _clearDropTarget;

    private Point _dragStartPoint;
    private Point _dragPointerOffset;

    private ScheduleItem? _dragSchedule;
    private DateTime _dragDisplayDate;
    private string _dragDisplayText = string.Empty;

    private FrameworkElement? _dragSourceElement;

    private bool _isDragging;
    private double _dragSourceOriginalOpacity = 1.0;

    /// <summary>
    /// 현재 실제 Drag가 진행 중인지 여부입니다.
    /// </summary>
    public bool IsDragging => _isDragging;

    /// <summary>
    /// 현재 클릭 또는 Drag 후보가 존재하는지 여부입니다.
    /// </summary>
    public bool HasCandidate => _dragSchedule is not null;

    public ScheduleDragController(FrameworkElement dragSurface, ScheduleDragPreview dragPreview, Action clearDropTarget)
    {
        _dragSurface = dragSurface ?? throw new ArgumentNullException(nameof(dragSurface));
        _dragPreview = dragPreview ?? throw new ArgumentNullException(nameof(dragPreview));
        _clearDropTarget = clearDropTarget ?? throw new ArgumentNullException(nameof(clearDropTarget));
    }

    /// <summary>
    /// 사용자가 일정 요소를 눌렀을 때 클릭 또는 Drag 후보 상태를 준비합니다.
    /// 공통 일정 ViewModel이 아닌 요소라면 false를 반환합니다.
    /// </summary>
    public bool TryPrepareCandidate(FrameworkElement sourceElement, object? dataContext, MouseButtonEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(sourceElement);
        ArgumentNullException.ThrowIfNull(e);

        if (dataContext is not SpanningScheduleViewModelBase scheduleViewModel)
        {
            return false;
        }

        _dragStartPoint = e.GetPosition(_dragSurface);
        _dragPointerOffset = e.GetPosition(sourceElement);

        _dragSchedule = scheduleViewModel.Schedule;
        _dragDisplayDate = scheduleViewModel.DisplayDate;
        _dragDisplayText = scheduleViewModel.DisplayText;
        _dragSourceElement = sourceElement;

        return true;
    }

    /// <summary>
    /// 마우스 이동을 처리하고 필요한 경우 실제 Drag를 시작합니다.
    /// Drag 중이면 미리보기를 이동하고 현재 달력 위치를 콜백으로 전달합니다.
    /// </summary>
    public bool HandleMouseMove(MouseEventArgs e, IInputElement calendarArea, Action<Point> updateDropTarget)
    {
        ArgumentNullException.ThrowIfNull(e);
        ArgumentNullException.ThrowIfNull(calendarArea);
        ArgumentNullException.ThrowIfNull(updateDropTarget);

        if (_dragSchedule is null || _dragSourceElement is null)
        {
            return false;
        }

        if (e.LeftButton != MouseButtonState.Pressed)
        {
            if (_isDragging)
            {
                Cancel();
            }
            else
            {
                ClearCandidate();
            }

            return false;
        }

        var currentPosition = e.GetPosition(_dragSurface);

        if (!_isDragging)
        {
            var horizontalDistance = Math.Abs(currentPosition.X - _dragStartPoint.X);
            var verticalDistance = Math.Abs(currentPosition.Y - _dragStartPoint.Y);

            if (horizontalDistance < SystemParameters.MinimumHorizontalDragDistance &&
                verticalDistance < SystemParameters.MinimumVerticalDragDistance)
            {
                return false;
            }

            if (!BeginDrag(currentPosition))
            {
                return false;
            }
        }

        _dragPreview.Move(currentPosition, _dragPointerOffset);

        updateDropTarget(e.GetPosition(calendarArea));

        return true;
    }

    /// <summary>
    /// MouseUp 시 현재 클릭 또는 Drag 결과를 반환하고 내부 상태를 정리합니다.
    /// </summary>
    public bool TryCompleteRelease(out ScheduleDragReleaseResult result)
    {
        if (_dragSchedule is null)
        {
            result = default;
            return false;
        }

        result = new ScheduleDragReleaseResult(
            _dragSchedule,
            _dragDisplayDate,
            _isDragging);

        if (_isDragging)
        {
            EndDragVisuals();
        }
        else
        {
            ClearCandidate();
        }

        return true;
    }

    /// <summary>
    /// 진행 중인 Drag 또는 준비된 Drag 후보 상태를 취소하고 모두 초기화합니다.
    /// </summary>
    public void Cancel()
    {
        if (_isDragging)
        {
            EndDragVisuals();
            return;
        }

        _dragPreview.Hide();
        Mouse.OverrideCursor = null;

        _clearDropTarget();
        ClearCandidate();
    }

    /// <summary>
    /// View가 화면에서 제거될 때 남아 있는 Drag 관련 상태를 모두 정리합니다.
    /// </summary>
    public void Cleanup()
    {
        Cancel();
    }

    /// <summary>
    /// 실제 Drag를 시작하고 Mouse Capture와 미리보기를 활성화합니다.
    /// </summary>
    private bool BeginDrag(Point currentPosition)
    {
        if (_dragSchedule is null || _dragSourceElement is null)
        {
            return false;
        }

        if (!Mouse.Capture(_dragSurface, CaptureMode.SubTree))
        {
            ClearCandidate();
            return false;
        }

        _isDragging = true;

        _dragSourceOriginalOpacity = _dragSourceElement.Opacity;
        _dragSourceElement.Opacity = 0.4;

        _dragPreview.Show(
            _dragDisplayText,
            _dragSourceElement.ActualWidth,
            _dragSourceElement.ActualHeight);

        _dragPreview.Move(currentPosition, _dragPointerOffset);

        Mouse.OverrideCursor = Cursors.SizeAll;

        _dragSurface.Focus();

        return true;
    }

    /// <summary>
    /// 실제 Drag 화면 상태를 원래대로 복원한 뒤 내부 상태를 초기화합니다.
    /// </summary>
    private void EndDragVisuals()
    {
        _dragPreview.Hide();

        if (_dragSourceElement is not null)
        {
            _dragSourceElement.Opacity = _dragSourceOriginalOpacity;
        }

        _clearDropTarget();

        Mouse.OverrideCursor = null;

        _isDragging = false;

        if (Mouse.Captured == _dragSurface)
        {
            Mouse.Capture(null);
        }

        ClearCandidate();
    }

    /// <summary>
    /// 클릭 또는 Drag 후보 정보를 초기화합니다.
    /// </summary>
    private void ClearCandidate()
    {
        _dragSchedule = null;
        _dragDisplayDate = default;
        _dragDisplayText = string.Empty;

        _dragSourceElement = null;

        _dragStartPoint = default;
        _dragPointerOffset = default;

        _dragSourceOriginalOpacity = 1.0;
    }
}

/// <summary>
/// MouseUp 시 완료된 일정 입력이 단순 클릭이었는지
/// 실제 Drag였는지와 이동 계산에 필요한 정보를 전달합니다.
/// </summary>
public readonly record struct ScheduleDragReleaseResult(
    ScheduleItem Schedule,
    DateTime DisplayDate,
    bool WasDragging);