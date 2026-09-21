using DesktopSchedule.Models;

namespace DesktopSchedule.ViewModels;

/// <summary>
/// 주간 7일 화면의 특정 날짜에 표시할 일정 카드 정보를 제공합니다.
/// 여러 날짜 일정은 날짜마다 하나의 표시 카드가 만들어질 수 있지만,
/// 실제 일정 데이터는 모두 같은 ScheduleItem을 참조합니다.
/// </summary>
public class WeeklyScheduleCardViewModel
{
    /// <summary>
    /// 실제 일정 데이터입니다.
    /// </summary>
    public ScheduleItem Schedule { get; }

    /// <summary>
    /// 현재 카드가 표시되는 날짜입니다.
    /// 여러 날짜 일정의 Drag 이동 기준으로도 사용합니다.
    /// </summary>
    public DateTime DisplayDate { get; }

    /// <summary>
    /// 일정 제목입니다.
    /// </summary>
    public string Title => Schedule.Title;

    /// <summary>
    /// 하루 종일 일정인지 여부입니다.
    /// </summary>
    public bool IsAllDay => Schedule.IsAllDay;

    /// <summary>
    /// 두 날짜 이상에 걸쳐 있는 일정인지 여부입니다.
    /// </summary>
    public bool IsMultiDay => Schedule.StartAt.Date != Schedule.EndAt.Date;

    /// <summary>
    /// 카드 앞부분에 표시할 시간 또는 일정 상태입니다.
    /// </summary>
    public string TimeText
    {
        get
        {
            if (Schedule.IsAllDay)
            {
                return "종일";
            }

            if (!IsMultiDay || DisplayDate == Schedule.StartAt.Date)
            {
                return Schedule.StartAt.ToString("HH:mm");
            }

            if (DisplayDate == Schedule.EndAt.Date)
            {
                return $"~{Schedule.EndAt:HH:mm}";
            }

            return "계속";
        }
    }

    /// <summary>
    /// 주간 일정 카드에 표시할 최종 문자열입니다.
    /// </summary>
    public string DisplayText => $"{TimeText} {Title}";

    /// <summary>
    /// 마우스를 올렸을 때 일정의 전체 날짜와 시간을 표시합니다.
    /// </summary>
    public string ToolTipText
    {
        get
        {
            if (Schedule.IsAllDay)
            {
                return Schedule.StartAt.Date == Schedule.EndAt.Date
                    ? $"{Schedule.StartAt:yyyy-MM-dd} · 하루 종일"
                    : $"{Schedule.StartAt:yyyy-MM-dd} ~ {Schedule.EndAt:yyyy-MM-dd} · 하루 종일";
            }

            return Schedule.StartAt.Date == Schedule.EndAt.Date
                ? $"{Schedule.StartAt:yyyy-MM-dd HH:mm} ~ {Schedule.EndAt:HH:mm}"
                : $"{Schedule.StartAt:yyyy-MM-dd HH:mm} ~ {Schedule.EndAt:yyyy-MM-dd HH:mm}";
        }
    }

    public WeeklyScheduleCardViewModel(ScheduleItem schedule, DateTime displayDate)
    {
        Schedule = schedule ?? throw new ArgumentNullException(nameof(schedule));
        DisplayDate = displayDate.Date;
    }
}