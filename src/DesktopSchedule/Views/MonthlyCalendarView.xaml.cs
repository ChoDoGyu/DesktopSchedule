using System.Windows; // FrameworkElement를 사용하기 위해 필요합니다.
using System.Windows.Controls; // UserControl 클래스를 사용하기 위해 필요합니다.
using System.Windows.Input; // MouseButtonEventArgs를 사용하기 위해 필요합니다.
using DesktopSchedule.Models; // ScheduleItem을 사용하기 위해 필요합니다.
using DesktopSchedule.ViewModels; // 월간 달력 ViewModel 타입을 사용하기 위해 필요합니다.

namespace DesktopSchedule.Views;

/// <summary>
/// 월간 달력 화면을 표시하고,
/// 날짜 셀과 일정 요소의 마우스 상호작용을 ViewModel Command에 연결합니다.
/// </summary>
public partial class MonthlyCalendarView : UserControl
{
    public MonthlyCalendarView()
    {
        // XAML에 정의된 UI 요소를 생성하고 연결합니다.
        InitializeComponent();
    }

    /// <summary>
    /// 사용자가 월간 달력의 날짜 셀을 클릭했을 때 호출됩니다.
    /// 먼저 해당 날짜를 선택한 뒤 새 일정 편집기를 열어
    /// 클릭한 날짜가 새 일정의 시작/종료 기본 날짜가 되도록 합니다.
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

        // 날짜 클릭 이벤트가 상위 요소까지 전달되어
        // 동일 동작이 중복 실행되는 것을 방지합니다.
        e.Handled = true;
    }

    /// <summary>
    /// 월간 달력의 단일 날짜 일정 또는 여러 날짜 연결 일정을 클릭했을 때 호출됩니다.
    /// 화면용 ViewModel에서 실제 ScheduleItem을 얻어 기존 일정 편집 Command에 전달합니다.
    /// </summary>
    private void Schedule_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Left)
        {
            return;
        }

        if (sender is not FrameworkElement element)
        {
            return;
        }

        var schedule = GetSchedule(element.DataContext);

        if (schedule is null)
        {
            return;
        }

        if (DataContext is not MonthlyCalendarViewModel viewModel)
        {
            return;
        }

        if (viewModel.SelectScheduleCommand.CanExecute(schedule))
        {
            viewModel.SelectScheduleCommand.Execute(schedule);
        }

        // 단일 날짜 일정은 날짜 셀 내부에 있으므로,
        // 이벤트가 날짜 셀까지 전달되면 새 일정 편집기가 다시 열릴 수 있습니다.
        // 일정 클릭을 처리한 뒤 반드시 이벤트 전달을 중단합니다.
        e.Handled = true;
    }

    /// <summary>
    /// 월간 달력에서 사용하는 두 종류의 일정 표시 ViewModel에서
    /// 공통으로 실제 ScheduleItem을 추출합니다.
    /// </summary>
    private static ScheduleItem? GetSchedule(object? dataContext)
    {
        return dataContext switch
        {
            MonthlyScheduleCardViewModel card => card.Schedule,
            MonthlySpanningScheduleViewModel spanning => spanning.Schedule,
            _ => null
        };
    }
}