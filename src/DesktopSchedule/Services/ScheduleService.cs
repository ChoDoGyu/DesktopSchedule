using DesktopSchedule.Models; // ScheduleItem을 사용하기 위해 필요합니다.
using DesktopSchedule.Repositories; // IScheduleRepository를 사용하기 위해 필요합니다.

namespace DesktopSchedule.Services;

/// <summary>
/// 일정의 생성, 조회, 수정, 삭제, 완료 상태와 관련된 비즈니스 로직을 관리합니다.
/// </summary>
public class ScheduleService
{
    // 실제 일정 데이터를 저장하고 불러올 Repository입니다.
    private readonly IScheduleRepository _scheduleRepository;

    public ScheduleService(IScheduleRepository scheduleRepository)
    {
        // Repository가 전달되지 않으면 ScheduleService를 정상적으로 사용할 수 없으므로
        // 즉시 예외를 발생시킵니다.
        _scheduleRepository = scheduleRepository ?? throw new ArgumentNullException(nameof(scheduleRepository));
    }

    /// <summary>
    /// 저장된 일정을 반환합니다.
    /// 기본적으로 완료된 일정은 제외합니다.
    /// </summary>
    public IReadOnlyList<ScheduleItem> GetAll(bool includeCompleted = false)
    {
        var schedules = _scheduleRepository.GetAll();

        // 완료 일정까지 표시하도록 요청한 경우 전체 목록을 그대로 반환합니다.
        if (includeCompleted)
        {
            return schedules;
        }

        // 기본 화면에서는 완료되지 않은 일정만 반환합니다.
        return schedules
            .Where(schedule => !schedule.IsCompleted)
            .ToList();
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
        // 현재 일정 추가 UI가 아직 알림 입력을 지원하지 않으므로
        // 알림 없음 상태로 저장합니다.
        return Add(
            title,
            description,
            startAt,
            endAt,
            isAllDay,
            false,
            0);
    }

    /// <summary>
    /// 알림 설정을 포함하여 새로운 일정을 생성하고 저장합니다.
    /// </summary>
    public ScheduleItem Add(
        string title,
        string description,
        DateTime startAt,
        DateTime endAt,
        bool isAllDay,
        bool isReminderEnabled,
        int reminderMinutesBefore)
    {
        // 일정으로 사용할 수 있는 값인지 먼저 검사합니다.
        Validate(
            title,
            startAt,
            endAt,
            isReminderEnabled,
            reminderMinutesBefore);

        // 생성 시점과 수정 시점을 동일한 값으로 사용하기 위해
        // 현재 시간을 한 번만 가져옵니다.
        var now = DateTime.Now;

        var schedule = new ScheduleItem
        {
            Title = title.Trim(),
            Description = description.Trim(),
            StartAt = startAt,
            EndAt = endAt,
            IsAllDay = isAllDay,

            // 새 일정은 항상 미완료 상태로 시작합니다.
            IsCompleted = false,
            CompletedAt = null,

            IsReminderEnabled = isReminderEnabled,

            // 알림을 사용하지 않을 때는 불필요한 값을 남기지 않고 0으로 정리합니다.
            ReminderMinutesBefore = isReminderEnabled
                ? reminderMinutesBefore
                : 0,

            CreatedAt = now,
            UpdatedAt = now
        };

        // 검증이 끝난 일정을 Repository를 통해 SQLite에 저장합니다.
        _scheduleRepository.Add(schedule);

        return schedule;
    }

    /// <summary>
    /// 기존 일정의 내용을 수정합니다.
    /// 완료 상태와 완료 시간은 변경하지 않습니다.
    /// </summary>
    public void Update(
        Guid id,
        string title,
        string description,
        DateTime startAt,
        DateTime endAt,
        bool isAllDay,
        bool isReminderEnabled,
        int reminderMinutesBefore)
    {
        // 수정할 값도 새 일정과 동일한 규칙으로 검사합니다.
        Validate(
            title,
            startAt,
            endAt,
            isReminderEnabled,
            reminderMinutesBefore);

        // 먼저 기존 일정을 조회합니다.
        var schedule = _scheduleRepository.GetById(id);

        if (schedule is null)
        {
            throw new KeyNotFoundException($"수정할 일정을 찾을 수 없습니다. Id: {id}");
        }

        // 일정의 편집 가능한 값을 변경합니다.
        // CreatedAt, IsCompleted, CompletedAt은 기존 값을 유지합니다.
        schedule.Title = title.Trim();
        schedule.Description = description.Trim();
        schedule.StartAt = startAt;
        schedule.EndAt = endAt;
        schedule.IsAllDay = isAllDay;
        schedule.IsReminderEnabled = isReminderEnabled;
        schedule.ReminderMinutesBefore = isReminderEnabled
            ? reminderMinutesBefore
            : 0;

        schedule.UpdatedAt = DateTime.Now;

        // 변경된 일정 정보를 SQLite에 반영합니다.
        _scheduleRepository.Update(schedule);
    }

    /// <summary>
    /// 지정한 일정을 완료 상태로 변경합니다.
    /// </summary>
    public void MarkAsCompleted(Guid id)
    {
        var schedule = GetRequiredSchedule(id, "완료 처리할");

        // 이미 완료된 일정이라면 다시 완료 시간을 변경하지 않습니다.
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

        // 이미 미완료 상태라면 추가 변경이 필요하지 않습니다.
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
        // 존재하지 않는 일정을 삭제하려는 실수를 구분하기 위해 먼저 조회합니다.
        GetRequiredSchedule(id, "삭제할");

        _scheduleRepository.Delete(id);
    }

    /// <summary>
    /// 지정한 Id의 일정을 반환하며, 존재하지 않으면 예외를 발생시킵니다.
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
    private static void Validate(
        string title,
        DateTime startAt,
        DateTime endAt,
        bool isReminderEnabled,
        int reminderMinutesBefore)
    {
        // 공백만 입력한 경우도 제목이 없는 것으로 처리합니다.
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("일정 제목은 비어 있을 수 없습니다.", nameof(title));
        }

        // 종료 시간이 시작 시간보다 앞설 수는 없습니다.
        if (endAt < startAt)
        {
            throw new ArgumentException("일정 종료 시간은 시작 시간보다 빠를 수 없습니다.", nameof(endAt));
        }

        // 알림을 사용하는 경우 시작 이후의 시간을 의미하는 음수 값은 허용하지 않습니다.
        if (isReminderEnabled && reminderMinutesBefore < 0)
        {
            throw new ArgumentException(
                "알림 시간은 0분 이상이어야 합니다.",
                nameof(reminderMinutesBefore));
        }
    }
}