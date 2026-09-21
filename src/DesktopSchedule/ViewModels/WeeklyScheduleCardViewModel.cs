using DesktopSchedule.Models;

namespace DesktopSchedule.ViewModels;

/// <summary>
/// 주간 화면에서 사용하는 단일 날짜 일정 카드입니다.
/// 공통 일정 카드 정보와 표시 규칙은 ScheduleCardViewModelBase에서 제공합니다.
/// </summary>
public class WeeklyScheduleCardViewModel : ScheduleCardViewModelBase
{
    public WeeklyScheduleCardViewModel(ScheduleItem schedule, DateTime displayDate) : base(schedule, displayDate)
    {
    }
}