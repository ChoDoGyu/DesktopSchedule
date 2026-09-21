using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DesktopSchedule.Models;
using DesktopSchedule.ViewModels;

namespace DesktopSchedule.Views;

/// <summary>
/// 주간 일정 화면의 View입니다.
/// 마우스 좌표가 필요한 Drag & Drop 입력을 처리합니다.
/// </summary>
public partial class WeeklyScheduleView : UserControl
{
    // 일정 Drag 여부를 판별하기 위해 마우스를 처음 누른 위치를 저장합니다.
    private Point _dragStartPoint;

    // 현재 Drag 대상으로 준비된 일정입니다.
    private ScheduleItem? _dragSchedule;

    public WeeklyScheduleView()
    {
        InitializeComponent();
    }

    /// <summary>
    /// 일정 블록에서 왼쪽 마우스 버튼을 눌렀을 때 Drag 준비 상태를 만듭니다.
    /// </summary>
    private void ScheduleButton_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not Button button || button.DataContext is not WeeklyTimedScheduleViewModel timedSchedule)
        {
            return;
        }

        _dragStartPoint = e.GetPosition(this);
        _dragSchedule = timedSchedule.Schedule;
    }

    /// <summary>
    /// 마우스를 일정 거리 이상 움직이면 실제 Drag & Drop을 시작합니다.
    /// </summary>
    private void ScheduleButton_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed || _dragSchedule is null)
        {
            return;
        }

        var currentPosition = e.GetPosition(this);
        var horizontalDistance = Math.Abs(currentPosition.X - _dragStartPoint.X);
        var verticalDistance = Math.Abs(currentPosition.Y - _dragStartPoint.Y);

        if (horizontalDistance < SystemParameters.MinimumHorizontalDragDistance &&
            verticalDistance < SystemParameters.MinimumVerticalDragDistance)
        {
            return;
        }

        var schedule = _dragSchedule;

        try
        {
            var data = new DataObject(typeof(ScheduleItem), schedule);
            DragDrop.DoDragDrop((DependencyObject)sender, data, DragDropEffects.Move);
        }
        finally
        {
            _dragSchedule = null;
        }

        e.Handled = true;
    }

    /// <summary>
    /// 일정 데이터가 시간표 위에 Drag되고 있으면 Move 동작임을 표시합니다.
    /// </summary>
    private void TimelineScheduleArea_DragOver(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(typeof(ScheduleItem)))
        {
            e.Effects = DragDropEffects.None;
            e.Handled = true;
            return;
        }

        e.Effects = DragDropEffects.Move;
        e.Handled = true;
    }

    /// <summary>
    /// 시간표에 Drop된 위치를 요일 인덱스와 Y 위치로 변환해 ViewModel에 전달합니다.
    /// </summary>
    private void TimelineScheduleArea_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetData(typeof(ScheduleItem)) is not ScheduleItem schedule)
        {
            return;
        }

        if (DataContext is not WeeklyScheduleViewModel viewModel)
        {
            return;
        }

        if (TimelineScheduleArea.ActualWidth <= 0)
        {
            return;
        }

        var position = e.GetPosition(TimelineScheduleArea);
        var dayColumnWidth = TimelineScheduleArea.ActualWidth / 7.0;

        var targetDayIndex = (int)(position.X / dayColumnWidth);
        targetDayIndex = Math.Clamp(targetDayIndex, 0, 6);

        viewModel.MoveScheduleByDrop(schedule, targetDayIndex, position.Y);

        e.Effects = DragDropEffects.Move;
        e.Handled = true;
    }
}