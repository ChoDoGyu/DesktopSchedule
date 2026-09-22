using System.Windows;
using System.Windows.Threading;
using DesktopSchedule.Models;

namespace DesktopSchedule.Views;

/// <summary>
/// 일정 알림이 발생했을 때 화면 오른쪽 아래에 표시되는 팝업 창입니다.
/// 여러 알림이 동시에 발생하면 오른쪽 아래부터 위쪽으로 차례대로 쌓아 표시합니다.
/// </summary>
public partial class ReminderPopupWindow : Window
{
    private static readonly TimeSpan DisplayDuration = TimeSpan.FromSeconds(5);
    private static readonly List<ReminderPopupWindow> ActivePopups = new();

    private const double ScreenMargin = 16.0;
    private const double PopupSpacing = 8.0;

    private readonly DispatcherTimer _closeTimer;

    public ReminderPopupWindow(ScheduleItem schedule)
    {
        ArgumentNullException.ThrowIfNull(schedule);

        InitializeComponent();

        ScheduleTitleTextBlock.Text = schedule.Title;
        ScheduleTimeTextBlock.Text = GetScheduleTimeText(schedule);

        _closeTimer = new DispatcherTimer
        {
            Interval = DisplayDuration
        };

        _closeTimer.Tick += CloseTimer_Tick;

        Loaded += ReminderPopupWindow_Loaded;
        Closed += ReminderPopupWindow_Closed;
    }

    /// <summary>
    /// 창 크기가 확정되면 현재 표시 중인 알림 목록에 추가하고,
    /// 오른쪽 아래부터 위쪽으로 모든 팝업의 위치를 다시 계산합니다.
    /// </summary>
    private void ReminderPopupWindow_Loaded(object sender, RoutedEventArgs e)
    {
        ActivePopups.Add(this);

        RepositionActivePopups();

        _closeTimer.Start();
    }

    /// <summary>
    /// 지정된 표시 시간이 지나면 알림 팝업을 닫습니다.
    /// </summary>
    private void CloseTimer_Tick(object? sender, EventArgs e)
    {
        _closeTimer.Stop();
        Close();
    }

    /// <summary>
    /// 팝업이 닫히면 활성 목록에서 제거하고,
    /// 남아 있는 팝업들이 빈 공간을 채우도록 다시 배치합니다.
    /// </summary>
    private void ReminderPopupWindow_Closed(object? sender, EventArgs e)
    {
        _closeTimer.Stop();
        _closeTimer.Tick -= CloseTimer_Tick;

        Loaded -= ReminderPopupWindow_Loaded;
        Closed -= ReminderPopupWindow_Closed;

        ActivePopups.Remove(this);

        RepositionActivePopups();
    }

    /// <summary>
    /// 현재 표시 중인 모든 알림을 작업 영역 오른쪽 아래부터 위쪽으로 배치합니다.
    /// 먼저 표시된 알림이 가장 아래에 위치하고 새 알림은 그 위에 추가됩니다.
    /// </summary>
    private static void RepositionActivePopups()
    {
        var workArea = SystemParameters.WorkArea;
        var currentBottom = workArea.Bottom - ScreenMargin;

        foreach (var popup in ActivePopups)
        {
            popup.Left = workArea.Right - popup.ActualWidth - ScreenMargin;
            popup.Top = currentBottom - popup.ActualHeight;

            currentBottom = popup.Top - PopupSpacing;
        }
    }

    /// <summary>
    /// 알림 팝업 하단에 표시할 일정 시작 정보를 생성합니다.
    /// </summary>
    private static string GetScheduleTimeText(ScheduleItem schedule)
    {
        if (schedule.IsAllDay)
        {
            return $"{schedule.StartAt:yyyy.MM.dd} · 하루 종일";
        }

        return $"{schedule.StartAt:yyyy.MM.dd HH:mm} 시작";
    }
}