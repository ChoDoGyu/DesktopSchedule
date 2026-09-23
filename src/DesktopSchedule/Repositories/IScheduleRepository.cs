using DesktopSchedule.Models; // ScheduleItem을 사용하기 위해 필요합니다.

namespace DesktopSchedule.Repositories;

/// <summary>
/// 일정 데이터를 저장하고 불러오기 위한 기능의 규약을 정의합니다.
/// </summary>
public interface IScheduleRepository
{
    /// <summary>
    /// 저장된 모든 일정을 반환합니다.
    /// </summary>
    IReadOnlyList<ScheduleItem> GetAll();

    /// <summary>
    /// 지정한 기간과 겹치는 일정을 반환합니다.
    /// </summary>
    IReadOnlyList<ScheduleItem> GetByDateRange(DateTime rangeStart, DateTime rangeEndExclusive, bool includeCompleted);

    /// <summary>
    /// 지정한 알림 검사 구간에서 실제 알림 대상이 될 가능성이 있는 일정을 반환합니다.
    /// </summary>
    IReadOnlyList<ScheduleItem> GetReminderCandidates(DateTime reminderWindowStart, DateTime reminderWindowEnd);

    /// <summary>
    /// 지정한 Id와 일치하는 일정을 반환합니다.
    /// 존재하지 않으면 null을 반환합니다.
    /// </summary>
    ScheduleItem? GetById(Guid id);

    /// <summary>
    /// 새로운 일정을 저장합니다.
    /// </summary>
    void Add(ScheduleItem schedule);

    /// <summary>
    /// 기존 일정 정보를 수정합니다.
    /// </summary>
    void Update(ScheduleItem schedule);

    /// <summary>
    /// 지정한 Id의 일정을 삭제합니다.
    /// </summary>
    void Delete(Guid id);
}