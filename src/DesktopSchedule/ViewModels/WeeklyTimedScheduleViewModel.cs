using DesktopSchedule.Models; // ScheduleItem을 사용하기 위해 필요합니다.

namespace DesktopSchedule.ViewModels;

/// <summary>
/// 주간 시간표의 특정 날짜에 표시되는 일정 조각의 화면 정보를 관리합니다.
/// 하나의 일정이 자정이나 여러 날짜를 넘으면 날짜별로 여러 조각이 생성될 수 있습니다.
/// </summary>
public class WeeklyTimedScheduleViewModel
{
    /// <summary>
    /// 실제 일정 데이터입니다.
    /// 여러 조각으로 나뉘어도 모두 같은 ScheduleItem을 참조합니다.
    /// </summary>
    public ScheduleItem Schedule { get; }

    /// <summary>
    /// 이 일정 조각이 표시되는 날짜입니다.
    /// </summary>
    public DateTime Date { get; }

    /// <summary>
    /// 이 날짜 안에서 일정 조각이 시작되는 시각을 자정 기준 분으로 나타냅니다.
    /// </summary>
    public int StartMinutes { get; }

    /// <summary>
    /// 이 날짜 안에서 일정 조각이 끝나는 시각을 자정 기준 분으로 나타냅니다.
    /// 24:00은 1440입니다.
    /// </summary>
    public int EndMinutes { get; }

    /// <summary>
    /// 시간표에 표시할 일정 제목입니다.
    /// </summary>
    public string Title => Schedule.Title;

    /// <summary>
    /// 일정 블록의 위쪽 위치입니다.
    /// </summary>
    public double Top { get; }

    /// <summary>
    /// 일정 블록의 화면 표시 높이입니다.
    /// </summary>
    public double Height { get; }

    /// <summary>
    /// 일정 블록의 왼쪽 위치입니다.
    /// </summary>
    public double Left { get; private set; }

    /// <summary>
    /// 일정 블록의 가로 너비입니다.
    /// </summary>
    public double Width { get; private set; }

    /// <summary>
    /// 겹침 그룹 안에서 사용하는 열 번호입니다.
    /// </summary>
    public int ColumnIndex { get; private set; }

    /// <summary>
    /// 겹침 그룹에서 필요한 최대 열 개수입니다.
    /// </summary>
    public int ColumnCount { get; private set; } = 1;

    /// <summary>
    /// 마우스를 올렸을 때 실제 일정의 전체 날짜와 시간을 표시합니다.
    /// </summary>
    public string TimeText
    {
        get
        {
            if (Schedule.StartAt.Date == Schedule.EndAt.Date)
            {
                return $"{Schedule.StartAt:HH:mm} ~ {Schedule.EndAt:HH:mm}";
            }

            return $"{Schedule.StartAt:MM-dd HH:mm} ~ {Schedule.EndAt:MM-dd HH:mm}";
        }
    }

    public WeeklyTimedScheduleViewModel(ScheduleItem schedule, DateTime date, int startMinutes, int endMinutes, double top, double height)
    {
        Schedule = schedule ?? throw new ArgumentNullException(nameof(schedule));

        if (startMinutes < 0 || startMinutes >= 1440)
        {
            throw new ArgumentOutOfRangeException(nameof(startMinutes));
        }

        if (endMinutes <= 0 || endMinutes > 1440)
        {
            throw new ArgumentOutOfRangeException(nameof(endMinutes));
        }

        if (endMinutes <= startMinutes)
        {
            throw new ArgumentException("일정 조각의 종료 시각은 시작 시각보다 늦어야 합니다.");
        }

        Date = date.Date;
        StartMinutes = startMinutes;
        EndMinutes = endMinutes;
        Top = top;
        Height = height;
    }

    /// <summary>
    /// 겹치는 일정 계산 결과를 화면 배치 정보에 적용합니다.
    /// </summary>
    public void ApplyOverlapLayout(double left, double width, int columnIndex, int columnCount)
    {
        Left = left;
        Width = width;
        ColumnIndex = columnIndex;
        ColumnCount = columnCount;
    }
}