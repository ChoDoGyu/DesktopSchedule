using System.Collections.ObjectModel;

namespace DesktopSchedule.ViewModels;

/// <summary>
/// 월간 달력의 한 주를 나타냅니다.
/// 일요일부터 토요일까지 7개의 날짜 셀과
/// 해당 주를 가로질러 표시되는 여러 날짜 일정 막대를 관리합니다.
/// </summary>
public class MonthlyWeekViewModel
{
    /// <summary>
    /// 이 주의 시작 날짜입니다.
    /// 월간 달력은 일요일을 한 주의 시작으로 사용합니다.
    /// </summary>
    public DateTime WeekStartDate { get; }

    /// <summary>
    /// 이 주의 마지막 날짜입니다.
    /// WeekStartDate로부터 6일 뒤의 토요일입니다.
    /// </summary>
    public DateTime WeekEndDate => WeekStartDate.AddDays(6);

    /// <summary>
    /// 일요일부터 토요일까지 이 주를 구성하는 7개의 날짜 셀입니다.
    /// </summary>
    public ObservableCollection<MonthlyDayViewModel> Days { get; } = new();

    /// <summary>
    /// 이 주에서 여러 날짜를 가로질러 표시되는 연결 일정 막대 목록입니다.
    /// 실제 일정 하나가 여러 주에 걸치면 각 주마다 별도의 표시 막대가 생성됩니다.
    /// </summary>
    public ObservableCollection<MonthlySpanningScheduleViewModel> SpanningSchedules { get; } = new();

    /// <summary>
    /// 월간 달력의 한 주를 생성합니다.
    /// weekStartDate부터 7일을 생성하며,
    /// displayMonth를 기준으로 각 날짜가 현재 표시 월에 속하는지 판단합니다.
    /// </summary>
    public MonthlyWeekViewModel(DateTime weekStartDate, DateTime displayMonth)
    {
        WeekStartDate = weekStartDate.Date;

        for (var dayOffset = 0; dayOffset < 7; dayOffset++)
        {
            var date = WeekStartDate.AddDays(dayOffset);
            Days.Add(new MonthlyDayViewModel(date, displayMonth));
        }
    }

    /// <summary>
    /// 지정한 날짜가 이 주의 일요일부터 토요일 범위 안에 포함되는지 확인합니다.
    /// </summary>
    public bool ContainsDate(DateTime date)
    {
        var targetDate = date.Date;

        return targetDate >= WeekStartDate &&
               targetDate <= WeekEndDate;
    }

    /// <summary>
    /// 지정한 날짜에 해당하는 날짜 셀을 반환합니다.
    /// 이 주에 포함되지 않은 날짜라면 null을 반환합니다.
    /// </summary>
    public MonthlyDayViewModel? GetDay(DateTime date)
    {
        var targetDate = date.Date;

        foreach (var day in Days)
        {
            if (day.Date == targetDate)
            {
                return day;
            }
        }

        return null;
    }
}