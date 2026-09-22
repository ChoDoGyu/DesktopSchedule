using System.Windows.Threading;
using DesktopSchedule.Models;
using DesktopSchedule.Utilities;

namespace DesktopSchedule.Services;

/// <summary>
/// 애플리케이션 실행 중 저장된 일정의 알림 시각을 주기적으로 확인하고,
/// 알림 시간이 도달한 일정을 ReminderDue 이벤트로 전달합니다.
/// </summary>
public sealed class ReminderScheduler
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan MaximumReminderDelay = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan NotificationHistoryRetention = TimeSpan.FromDays(2);

    private readonly ScheduleService _scheduleService;
    private readonly DispatcherTimer _timer;

    // 현재 실행 중 이미 알림을 발생시킨 일정과 알림 시각을 저장합니다.
    // 같은 일정이라도 시작 시간이나 알림 설정이 변경되어 알림 시각이 달라지면 새로운 알림으로 처리합니다.
    private readonly HashSet<ReminderOccurrence> _notifiedReminders = new();

    private DateTime _lastCheckedAt;
    private bool _isRunning;

    /// <summary>
    /// 일정의 알림 시간이 도달했을 때 발생합니다.
    /// 실제 팝업 표시와 사운드 재생은 이 이벤트를 구독하는 쪽에서 담당합니다.
    /// </summary>
    public event EventHandler<ReminderDueEventArgs>? ReminderDue;

    public ReminderScheduler(ScheduleService scheduleService)
    {
        _scheduleService = scheduleService ?? throw new ArgumentNullException(nameof(scheduleService));

        _timer = new DispatcherTimer
        {
            Interval = CheckInterval
        };

        _timer.Tick += Timer_Tick;
    }

    /// <summary>
    /// 알림 일정 검사를 시작합니다.
    /// 중복 Start 호출은 무시합니다.
    /// </summary>
    public void Start()
    {
        if (_isRunning)
        {
            return;
        }

        _lastCheckedAt = DateTime.Now;
        _isRunning = true;
        _timer.Start();
    }

    /// <summary>
    /// 알림 일정 검사를 중지합니다.
    /// </summary>
    public void Stop()
    {
        if (!_isRunning)
        {
            return;
        }

        _timer.Stop();
        _isRunning = false;
    }

    /// <summary>
    /// 이전 검사 이후 현재 시각까지 알림 시간이 도달한 일정을 찾습니다.
    /// 동일한 일정과 동일한 알림 시각은 현재 실행 중 한 번만 발생시키며,
    /// 절전이나 장시간 중단으로 너무 오래 지난 알림은 뒤늦게 표시하지 않습니다.
    /// </summary>
    private void Timer_Tick(object? sender, EventArgs e)
    {
        var now = DateTime.Now;
        var schedules = _scheduleService.GetAll();

        foreach (var schedule in schedules)
        {
            try
            {
                ProcessSchedule(schedule, now);
            }
            catch (ArgumentOutOfRangeException)
            {
                // 특정 일정의 알림 데이터가 비정상이어도
                // 다른 정상 일정의 알림 검사는 계속 진행합니다.
            }
        }

        CleanupNotificationHistory(now);

        _lastCheckedAt = now;
    }

    /// <summary>
    /// 일정 하나의 알림 대상 여부와 발생 시각을 확인하고
    /// 현재 검사 구간에서 실제로 발생해야 하는 알림이면 이벤트를 전달합니다.
    /// </summary>
    private void ProcessSchedule(ScheduleItem schedule, DateTime now)
    {
        if (!ScheduleReminderCalculator.IsReminderTarget(schedule))
        {
            return;
        }

        var reminderAt = ScheduleReminderCalculator.GetReminderAt(schedule);

        if (reminderAt <= _lastCheckedAt || reminderAt > now)
        {
            return;
        }

        if (now - reminderAt > MaximumReminderDelay)
        {
            return;
        }

        var occurrence = new ReminderOccurrence(schedule.Id, reminderAt);

        if (!_notifiedReminders.Add(occurrence))
        {
            return;
        }

        ReminderDue?.Invoke(this, new ReminderDueEventArgs(schedule, reminderAt));
    }

    /// <summary>
    /// 장기간 애플리케이션을 실행할 때 중복 방지 기록이 계속 증가하지 않도록
    /// 충분히 오래 지난 알림 발생 기록을 제거합니다.
    /// </summary>
    private void CleanupNotificationHistory(DateTime now)
    {
        var retentionThreshold = now - NotificationHistoryRetention;

        _notifiedReminders.RemoveWhere(
            occurrence => occurrence.ReminderAt < retentionThreshold);
    }

    /// <summary>
    /// 일정 하나에서 특정 시각에 발생하는 알림 한 건을 식별합니다.
    /// </summary>
    private readonly record struct ReminderOccurrence(Guid ScheduleId, DateTime ReminderAt);
}

/// <summary>
/// 알림 시간이 도달한 일정과 실제 알림 시각을 전달합니다.
/// </summary>
public sealed class ReminderDueEventArgs : EventArgs
{
    public ScheduleItem Schedule { get; }

    public DateTime ReminderAt { get; }

    public ReminderDueEventArgs(ScheduleItem schedule, DateTime reminderAt)
    {
        Schedule = schedule ?? throw new ArgumentNullException(nameof(schedule));
        ReminderAt = reminderAt;
    }
}