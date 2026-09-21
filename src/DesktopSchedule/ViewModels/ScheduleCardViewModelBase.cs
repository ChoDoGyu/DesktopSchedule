using DesktopSchedule.Models;

namespace DesktopSchedule.ViewModels;

/// <summary>
/// 주간과 월간 달력의 단일 날짜 일정 카드가 공통으로 사용하는
/// 일정 데이터와 표시 정보를 제공합니다.
/// </summary>
/// <remarks>
/// 실제 일정 데이터는 ScheduleItem 하나를 그대로 참조하고,
/// 화면에서는 제목, 시간, 완료 상태, Tooltip 등의 표시 정보만 사용합니다.
/// </remarks>
public abstract class ScheduleCardViewModelBase
{
    /// <summary>
    /// 실제 일정 데이터입니다.
    /// 일정 수정, 삭제, 완료 처리와 Drag 이동에서 사용합니다.
    /// </summary>
    public ScheduleItem Schedule { get; }

    /// <summary>
    /// 현재 일정 카드가 표시되고 있는 날짜입니다.
    /// Drag 이동 시 사용자가 잡은 날짜를 계산하는 기준으로 사용합니다.
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
    /// 일정이 완료 상태인지 여부입니다.
    /// 완료 일정의 화면 스타일을 구분할 때 사용합니다.
    /// </summary>
    public bool IsCompleted => Schedule.IsCompleted;

    /// <summary>
    /// 실제 일정의 시작 날짜와 종료 날짜가 서로 다른지 여부입니다.
    /// </summary>
    public bool IsMultiDay => Schedule.StartAt.Date != Schedule.EndAt.Date;

    /// <summary>
    /// 일정 카드 앞부분에 표시할 시간 또는 상태 문자열입니다.
    /// </summary>
    public string TimeText
    {
        get
        {
            if (IsAllDay)
            {
                return "종일";
            }

            // 단일 날짜 일정이거나 현재 카드가 실제 시작 날짜에 표시되는 경우
            // 일정의 시작 시간을 표시합니다.
            if (!IsMultiDay || DisplayDate == Schedule.StartAt.Date)
            {
                return Schedule.StartAt.ToString("HH:mm");
            }

            // 일반적으로 여러 날짜 일정은 연결 막대로 표시되지만,
            // 경계 상황에서도 의미 있는 문자열을 제공하도록 처리합니다.
            if (DisplayDate == Schedule.EndAt.Date)
            {
                return $"~{Schedule.EndAt:HH:mm}";
            }

            return "계속";
        }
    }

    /// <summary>
    /// 일정 카드에 실제로 표시할 최종 문자열입니다.
    /// </summary>
    public string DisplayText => $"{TimeText} {Title}";

    /// <summary>
    /// 마우스를 올렸을 때 표시할 일정의 전체 날짜와 시간 정보입니다.
    /// </summary>
    public string ToolTipText
    {
        get
        {
            if (IsAllDay)
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

    protected ScheduleCardViewModelBase(ScheduleItem schedule, DateTime displayDate)
    {
        Schedule = schedule ?? throw new ArgumentNullException(nameof(schedule));
        DisplayDate = displayDate.Date;
    }
}