using DesktopSchedule.Models; // ScheduleItem을 사용하기 위해 필요합니다.

namespace DesktopSchedule.ViewModels;

/// <summary>
/// 주간 화면의 특정 날짜에 표시되는 하나의 일정 항목을 나타냅니다.
/// </summary>
public class WeeklyScheduleEntryViewModel
{
    /// <summary>
    /// 원본 일정 데이터입니다.
    /// </summary>
    public ScheduleItem Schedule { get; }

    /// <summary>
    /// 이 일정 항목이 표시되는 날짜입니다.
    /// </summary>
    public DateTime Date { get; }

    /// <summary>
    /// 일정 제목입니다.
    /// </summary>
    public string Title => Schedule.Title;

    /// <summary>
    /// 일정이 여러 날짜에 걸쳐 있는지 여부입니다.
    /// </summary>
    public bool IsMultiDay => Schedule.StartAt.Date != Schedule.EndAt.Date;

    /// <summary>
    /// 현재 날짜가 일정의 시작 날짜인지 여부입니다.
    /// </summary>
    public bool IsStartDate => Schedule.StartAt.Date == Date;

    /// <summary>
    /// 현재 날짜가 일정의 종료 날짜인지 여부입니다.
    /// </summary>
    public bool IsEndDate => Schedule.EndAt.Date == Date;

    /// <summary>
    /// 현재 날짜에서 보여줄 일정 시간 또는 진행 상태입니다.
    /// </summary>
    public string DisplayTimeText
    {
        get
        {
            if (!IsMultiDay)
            {
                return Schedule.IsAllDay ? "하루 종일" : $"{Schedule.StartAt:HH:mm} ~ {Schedule.EndAt:HH:mm}";
            }

            if (IsStartDate)
            {
                return Schedule.IsAllDay ? "시작" : $"시작 {Schedule.StartAt:HH:mm}";
            }

            if (IsEndDate)
            {
                return Schedule.IsAllDay ? "종료" : $"종료 {Schedule.EndAt:HH:mm}";
            }

            return "계속";
        }
    }

    public WeeklyScheduleEntryViewModel(ScheduleItem schedule, DateTime date)
    {
        Schedule = schedule ?? throw new ArgumentNullException(nameof(schedule));
        Date = date.Date;
    }
}