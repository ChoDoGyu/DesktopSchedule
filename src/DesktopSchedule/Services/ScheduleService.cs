using DesktopSchedule.Models;
using DesktopSchedule.Repositories;

namespace DesktopSchedule.Services;

/// <summary>
/// 일정 데이터에 대한 생성, 조회, 수정, 삭제, 완료 처리 등의 비즈니스 로직을 담당합니다.
/// </summary>
public class ScheduleService
{
    private readonly IScheduleRepository _scheduleRepository;

    public ScheduleService(IScheduleRepository scheduleRepository)
    {
        _scheduleRepository = scheduleRepository ?? throw new ArgumentNullException(nameof(scheduleRepository));
    }

    /// <summary>
    /// 저장된 일정을 반환합니다.
    /// 기본적으로 완료된 일정은 제외하며, includeCompleted가 true이면 완료 일정도 함께 반환합니다.
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
    /// 일정이 존재하지 않으면 null을 반환합니다.
    /// </summary>
    public ScheduleItem? GetById(Guid id)
    {
        return _scheduleRepository.GetById(id);
    }

    /// <summary>
    /// 새로운 일정을 생성하고 저장합니다.
    /// </summary>
    public ScheduleItem Add(string title, string description, DateTime startAt, DateTime endAt, bool isAllDay, bool isReminderEnabled, int reminderMinutesBefore)
    {
        Validate(title, startAt, endAt, reminderMinutesBefore);

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
            ReminderMinutesBefore = reminderMinutesBefore,
            CreatedAt = now,
            UpdatedAt = now
        };

        _scheduleRepository.Add(schedule);

        return schedule;
    }

    /// <summary>
    /// 기존 일정의 내용을 수정합니다.
    /// </summary>
    public void Update(Guid id, string title, string description, DateTime startAt, DateTime endAt, bool isAllDay, bool isReminderEnabled, int reminderMinutesBefore)
    {
        Validate(title, startAt, endAt, reminderMinutesBefore);

        var schedule = GetRequiredSchedule(id);

        schedule.Title = title.Trim();
        schedule.Description = description.Trim();
        schedule.StartAt = startAt;
        schedule.EndAt = endAt;
        schedule.IsAllDay = isAllDay;
        schedule.IsReminderEnabled = isReminderEnabled;
        schedule.ReminderMinutesBefore = reminderMinutesBefore;
        schedule.UpdatedAt = DateTime.Now;

        _scheduleRepository.Update(schedule);
    }

    /// <summary>
    /// 일정의 전체 길이는 유지하면서 새로운 시작 날짜와 시간으로 이동합니다.
    /// 주간과 월간 Drag & Drop에서 공통으로 사용할 수 있습니다.
    /// </summary>
    public void Move(Guid id, DateTime newStartAt)
    {
        var schedule = GetRequiredSchedule(id);
        var duration = schedule.EndAt - schedule.StartAt;

        schedule.StartAt = newStartAt;
        schedule.EndAt = newStartAt + duration;
        schedule.UpdatedAt = DateTime.Now;

        _scheduleRepository.Update(schedule);
    }

    /// <summary>
    /// 지정한 일정을 실제 저장소에서 삭제합니다.
    /// </summary>
    public void Delete(Guid id)
    {
        GetRequiredSchedule(id);
        _scheduleRepository.Delete(id);
    }

    /// <summary>
    /// 일정을 완료 상태로 변경합니다.
    /// </summary>
    public void MarkAsCompleted(Guid id)
    {
        var schedule = GetRequiredSchedule(id);

        if (schedule.IsCompleted)
        {
            return;
        }

        schedule.IsCompleted = true;
        schedule.CompletedAt = DateTime.Now;
        schedule.UpdatedAt = DateTime.Now;

        _scheduleRepository.Update(schedule);
    }

    /// <summary>
    /// 완료된 일정을 다시 미완료 상태로 변경합니다.
    /// </summary>
    public void MarkAsIncomplete(Guid id)
    {
        var schedule = GetRequiredSchedule(id);

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
    /// 반드시 존재해야 하는 일정을 조회합니다.
    /// </summary>
    private ScheduleItem GetRequiredSchedule(Guid id)
    {
        var schedule = _scheduleRepository.GetById(id);

        if (schedule is null)
        {
            throw new KeyNotFoundException($"일정을 찾을 수 없습니다. Id: {id}");
        }

        return schedule;
    }

    /// <summary>
    /// 일정 입력값이 저장 가능한 상태인지 확인합니다.
    /// </summary>
    private static void Validate(string title, DateTime startAt, DateTime endAt, int reminderMinutesBefore)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("일정 제목은 비어 있을 수 없습니다.", nameof(title));
        }

        if (endAt < startAt)
        {
            throw new ArgumentException("일정 종료 시간은 시작 시간보다 빠를 수 없습니다.", nameof(endAt));
        }

        if (reminderMinutesBefore < 0)
        {
            throw new ArgumentException("알림 시간은 0분 이상이어야 합니다.", nameof(reminderMinutesBefore));
        }
    }
}