using DesktopSchedule.Models;

namespace DesktopSchedule.Utilities;

/// <summary>
/// 일정의 알림 대상 여부와 실제 알림 발생 시각을 계산합니다.
/// 화면이나 타이머 상태를 가지지 않는 순수 계산 전용 클래스입니다.
/// </summary>
public static class ScheduleReminderCalculator
{
    /// <summary>
    /// 지정한 일정이 현재 알림 기능의 대상인지 확인합니다.
    /// 알림이 활성화되어 있고 완료되지 않은 일정만 대상입니다.
    /// </summary>
    public static bool IsReminderTarget(ScheduleItem schedule)
    {
        ArgumentNullException.ThrowIfNull(schedule);

        return schedule.IsReminderEnabled && !schedule.IsCompleted;
    }

    /// <summary>
    /// 지정한 일정의 실제 알림 발생 시각을 계산합니다.
    /// 시간 일정은 일정 시작 시각에서 설정된 알림 분 수만큼 이전이며,
    /// 하루 종일 일정은 해당 날짜 오전 9시입니다.
    /// </summary>
    public static DateTime GetReminderAt(ScheduleItem schedule)
    {
        ArgumentNullException.ThrowIfNull(schedule);

        if (schedule.ReminderMinutesBefore < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(schedule), "알림 시간은 0분 이상이어야 합니다.");
        }

        if (schedule.IsAllDay)
        {
            return schedule.StartAt.Date.AddHours(9);
        }

        return schedule.StartAt.AddMinutes(-schedule.ReminderMinutesBefore);
    }
}