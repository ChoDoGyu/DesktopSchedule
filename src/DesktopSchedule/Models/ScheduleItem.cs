namespace DesktopSchedule.Models;

/// <summary>
/// 사용자가 등록한 하나의 일정 정보를 나타냅니다.
/// </summary>
public class ScheduleItem
{
    /// <summary>
    /// 일정을 고유하게 식별하는 값입니다.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// 일정 제목입니다.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 일정에 대한 추가 설명입니다.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 일정이 시작되는 날짜와 시간입니다.
    /// </summary>
    public DateTime StartAt { get; set; }

    /// <summary>
    /// 일정이 종료되는 날짜와 시간입니다.
    /// </summary>
    public DateTime EndAt { get; set; }

    /// <summary>
    /// 특정 시간이 아닌 하루 전체 일정인지 여부입니다.
    /// </summary>
    public bool IsAllDay { get; set; }

    /// <summary>
    /// 일정이 처음 생성된 날짜와 시간입니다.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    /// <summary>
    /// 일정이 마지막으로 수정된 날짜와 시간입니다.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}