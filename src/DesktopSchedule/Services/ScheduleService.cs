using DesktopSchedule.Models; // ScheduleItem을 사용하기 위해 필요합니다.
using DesktopSchedule.Repositories; // IScheduleRepository를 사용하기 위해 필요합니다.

namespace DesktopSchedule.Services;

/// <summary>
/// 일정의 생성, 조회, 수정, 이동, 삭제, 완료 상태와 관련된 비즈니스 로직을 관리합니다.
/// </summary>
public class ScheduleService
{
    // 실제 일정 데이터를 저장하고 불러올 Repository입니다.
    private readonly IScheduleRepository _scheduleRepository;

    public ScheduleService(IScheduleRepository scheduleRepository)
    {
        // Repository가 전달되지 않으면 ScheduleService를 정상적으로 사용할 수 없으므로 즉시 예외를 발생시킵니다.
        _scheduleRepository = scheduleRepository ?? throw new ArgumentNullException(nameof(scheduleRepository));
    }

    /// <summary>
    /// 저장된 일정을 반환합니다.
    /// 기본적으로 완료된 일정은 제외합니다.
    /// </summary>
    public IReadOnlyList<ScheduleItem> GetAll(bool includeCompleted = false)
    {
        var schedules = _scheduleRepository.GetAll();

        if (includeCompleted)
        {
            return schedules;
        }

        return schedules.Where(schedule => !schedule.IsCompleted).ToList();
    }

    /// <summary>
    /// 지정한 Id의 일정을 반환합니다.
    /// 존재하지 않으면 null을 반환합니다.
    /// </summary>
    public ScheduleItem? GetById(Guid id)
    {
        return _scheduleRepository.GetById(id);
    }

    /// <summary>
    /// 알림 설정 없이 새로운 일정을 생성하고 저장합니다.
    /// </summary>
    public ScheduleItem Add(string title, string description, DateTime startAt, DateTime endAt, bool isAllDay)
    {
        return Add(title, description, startAt, endAt, isAllDay, false, 0);
    }

    /// <summary>
    /// 알림 설정을 포함하여 새로운 일정을 생성하고 저장합니다.
    /// </summary>
    public ScheduleItem Add(string title, string description, DateTime startAt, DateTime endAt, bool isAllDay, bool isReminderEnabled, int reminderMinutesBefore)
    {
        Validate(title, startAt, endAt, isReminderEnabled, reminderMinutesBefore);

        var now = DateTime.Now;

        var schedule = new ScheduleItem
        {
            Title = title.Trim(),
            Description = description.Trim(),
            StartAt = startAt,
            EndAt = endAt,
            IsAllDay = isAllDay,
            IsCompleted = false,
            CompletedAt = null,
            IsReminderEnabled = isReminderEnabled,
            ReminderMinutesBefore = isReminderEnabled ? reminderMinutesBefore : 0,
            CreatedAt = now,
            UpdatedAt = now
        };

        _scheduleRepository.Add(schedule);

        return schedule;
    }

    /// <summary>
    /// 기존 일정의 내용을 수정합니다.
    /// 완료 상태와 완료 시간은 변경하지 않습니다.
    /// </summary>
    public void Update(Guid id, string title, string description, DateTime startAt, DateTime endAt, bool isAllDay, bool isReminderEnabled, int reminderMinutesBefore)
    {
        Validate(title, startAt, endAt, isReminderEnabled, reminderMinutesBefore);

        var schedule = GetRequiredSchedule(id, "수정할");

        schedule.Title = title.Trim();
        schedule.Description = description.Trim();
        schedule.StartAt = startAt;
        schedule.EndAt = endAt;
        schedule.IsAllDay = isAllDay;
        schedule.IsReminderEnabled = isReminderEnabled;
        schedule.ReminderMinutesBefore = isReminderEnabled ? reminderMinutesBefore : 0;
        schedule.UpdatedAt = DateTime.Now;

        _scheduleRepository.Update(schedule);
    }

    /// <summary>
    /// 일정의 전체 기간과 시간을 유지하면서 새로운 시작 시점으로 이동합니다.
    /// 주간과 월간 Drag & Drop에서 공통으로 사용합니다.
    /// </summary>
    public void Move(Guid id, DateTime newStartAt)
    {
        var schedule = GetRequiredSchedule(id, "이동할");
        var duration = schedule.EndAt - schedule.StartAt;

        schedule.StartAt = newStartAt;
        schedule.EndAt = newStartAt + duration;
        schedule.UpdatedAt = DateTime.Now;

        _scheduleRepository.Update(schedule);
    }

    /// <summary>
    /// 지정한 일정을 완료 상태로 변경합니다.
    /// </summary>
    public void MarkAsCompleted(Guid id)
    {
        var schedule = GetRequiredSchedule(id, "완료 처리할");

        if (schedule.IsCompleted)
        {
            return;
        }

        var now = DateTime.Now;

        schedule.IsCompleted = true;
        schedule.CompletedAt = now;
        schedule.UpdatedAt = now;

        _scheduleRepository.Update(schedule);
    }

    /// <summary>
    /// 지정한 일정의 완료 상태를 취소합니다.
    /// </summary>
    public void MarkAsIncomplete(Guid id)
    {
        var schedule = GetRequiredSchedule(id, "완료 취소할");

        if (!schedule.IsCompleted)
        {
            return;
        }

        schedule.IsCompleted = false;
        schedule.CompletedAt = null;
        schedule.UpdatedAt = DateTime.Now;

        _scheduleRepository.Update(schedule);
    }

    /// <summary>
    /// 지정한 Id의 일정을 영구적으로 삭제합니다.
    /// </summary>
    public void Delete(Guid id)
    {
        GetRequiredSchedule(id, "삭제할");
        _scheduleRepository.Delete(id);
    }

    /// <summary>
    /// 지정한 Id의 일정을 반환하며 존재하지 않으면 예외를 발생시킵니다.
    /// </summary>
    private ScheduleItem GetRequiredSchedule(Guid id, string operation)
    {
        var schedule = _scheduleRepository.GetById(id);

        if (schedule is null)
        {
            throw new KeyNotFoundException($"{operation} 일정을 찾을 수 없습니다. Id: {id}");
        }

        return schedule;
    }

    /// <summary>
    /// 일정에 필요한 기본 입력값이 유효한지 검사합니다.
    /// </summary>
    private static void Validate(string title, DateTime startAt, DateTime endAt, bool isReminderEnabled, int reminderMinutesBefore)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("일정 제목은 비어 있을 수 없습니다.", nameof(title));
        }

        if (endAt < startAt)
        {
            throw new ArgumentException("일정 종료 시간은 시작 시간보다 빠를 수 없습니다.", nameof(endAt));
        }

        if (isReminderEnabled && reminderMinutesBefore < 0)
        {
            throw new ArgumentException("알림 시간은 0분 이상이어야 합니다.", nameof(reminderMinutesBefore));
        }
    }
}