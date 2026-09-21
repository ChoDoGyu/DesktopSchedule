using DesktopSchedule.Models;

namespace DesktopSchedule.ViewModels;

/// <summary>
/// 월간 달력의 특정 날짜 셀 안에 표시되는 단일 날짜 일정 카드 정보를 제공합니다.
/// 실제 일정 데이터는 ScheduleItem을 그대로 참조하며,
/// 월간 화면에 필요한 표시 문자열과 Drag 기준 날짜만 추가로 제공합니다.
/// </summary>
public class MonthlyScheduleCardViewModel
{
    /// <summary>
    /// 실제 일정 데이터입니다.
    /// 수정, 삭제, Drag 이동 시 이 ScheduleItem의 Id를 사용합니다.
    /// </summary>
    public ScheduleItem Schedule { get; }

    /// <summary>
    /// 현재 일정 카드가 표시되고 있는 날짜입니다.
    /// Drag & Drop 시 사용자가 잡은 날짜와 Drop한 날짜 사이의 이동 일수를 계산할 때 사용합니다.
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
    /// 완료 일정 표시 기능을 사용할 때 화면 스타일을 구분하는 데 사용할 수 있습니다.
    /// </summary>
    public bool IsCompleted => Schedule.IsCompleted;

    /// <summary>
    /// 월간 일정 카드 앞부분에 표시할 시간 문자열입니다.
    /// 하루 종일 일정이면 시간을 표시하지 않고 "종일"을 표시합니다.
    /// </summary>
    public string TimeText => IsAllDay ? "종일" : Schedule.StartAt.ToString("HH:mm");

    /// <summary>
    /// 월간 날짜 셀 안에서 실제로 표시할 최종 문자열입니다.
    /// 예: "09:30 병원", "종일 생일"
    /// </summary>
    public string DisplayText => $"{TimeText} {Title}";

    /// <summary>
    /// 마우스를 올렸을 때 표시할 일정의 상세 시간 정보입니다.
    /// </summary>
    public string ToolTipText
    {
        get
        {
            if (IsAllDay)
            {
                return $"{Schedule.StartAt:yyyy-MM-dd} · 하루 종일";
            }

            return $"{Schedule.StartAt:yyyy-MM-dd HH:mm} ~ {Schedule.EndAt:HH:mm}";
        }
    }

    /// <summary>
    /// 월간 달력의 단일 날짜 일정 카드를 생성합니다.
    /// </summary>
    public MonthlyScheduleCardViewModel(ScheduleItem schedule, DateTime displayDate)
    {
        Schedule = schedule ?? throw new ArgumentNullException(nameof(schedule));
        DisplayDate = displayDate.Date;
    }
}