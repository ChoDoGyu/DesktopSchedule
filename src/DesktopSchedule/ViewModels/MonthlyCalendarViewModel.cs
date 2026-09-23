using System.Collections.ObjectModel;
using DesktopSchedule.Commands;
using DesktopSchedule.Models;
using DesktopSchedule.Services;
using DesktopSchedule.Utilities;

namespace DesktopSchedule.ViewModels;

/// <summary>
/// 월간 달력 화면의 6주 × 7일 구조와 월간 일정 배치를 관리합니다.
/// 일정 편집과 CRUD 공통 기능은 ScheduleEditorViewModelBase에서 제공하고,
/// 모든 일정의 날짜 범위와 Row 배치 계산은 ScheduleCalendarCalculator에서 제공합니다.
/// </summary>
public class MonthlyCalendarViewModel : ScheduleEditorViewModelBase
{
    // 현재 월간 달력에서 표시하고 있는 월입니다.
    // 날짜는 항상 해당 월의 1일로 정규화하여 관리합니다.
    private DateTime _displayMonth;

    // 사용자가 현재 선택한 날짜입니다.
    private DateTime _selectedDate;

    /// <summary>
    /// 현재 월간 달력에 표시되는 6개의 주입니다.
    /// 각 주는 일요일부터 토요일까지 7개의 날짜 셀을 가집니다.
    /// </summary>
    public ObservableCollection<MonthlyWeekViewModel> Weeks { get; } = new();

    /// <summary>
    /// 현재 월간 달력에서 표시하고 있는 월입니다.
    /// 값은 항상 해당 월의 1일입니다.
    /// </summary>
    public DateTime DisplayMonth
    {
        get => _displayMonth;
        private set
        {
            var normalizedMonth = new DateTime(value.Year, value.Month, 1);

            if (SetProperty(ref _displayMonth, normalizedMonth))
            {
                OnPropertyChanged(nameof(DisplayMonthText));
            }
        }
    }

    /// <summary>
    /// 월간 달력 상단에 표시할 연도와 월 문자열입니다.
    /// </summary>
    public string DisplayMonthText => $"{DisplayMonth:yyyy년 M월}";

    /// <summary>
    /// 사용자가 현재 선택한 날짜입니다.
    /// 시간 정보는 제거하고 날짜만 관리합니다.
    /// </summary>
    public DateTime SelectedDate
    {
        get => _selectedDate;
        private set => SetProperty(ref _selectedDate, value.Date);
    }

    /// <summary>
    /// 이전 달로 이동하는 Command입니다.
    /// </summary>
    public RelayCommand PreviousMonthCommand { get; }

    /// <summary>
    /// 오늘이 포함된 현재 달로 이동하는 Command입니다.
    /// </summary>
    public RelayCommand CurrentMonthCommand { get; }

    /// <summary>
    /// 다음 달로 이동하는 Command입니다.
    /// </summary>
    public RelayCommand NextMonthCommand { get; }

    /// <summary>
    /// 월간 달력에서 선택한 날짜를 현재 선택 날짜로 변경합니다.
    /// </summary>
    public RelayCommand SelectDateCommand { get; }

    public MonthlyCalendarViewModel(ScheduleService scheduleService) : base(scheduleService)
    {
        PreviousMonthCommand = new RelayCommand(_ => MoveMonth(-1));
        CurrentMonthCommand = new RelayCommand(_ => MoveToCurrentMonth());
        NextMonthCommand = new RelayCommand(_ => MoveMonth(1));
        SelectDateCommand = new RelayCommand(parameter => SelectDate(parameter as MonthlyDayViewModel));

        DisplayMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        SelectedDate = DateTime.Today;

        LoadMonth();
        LoadSchedules();
    }

    /// <summary>
    /// 공통 일정 편집 기능에서 사용할 현재 선택 날짜를 반환합니다.
    /// </summary>
    protected override DateTime GetSelectedDateForEditor()
    {
        return SelectedDate;
    }

    /// <summary>
    /// 일정 추가, 수정, 삭제, 완료 상태 변경 후
    /// 월간 일정 배치 전체를 다시 구성합니다.
    /// </summary>
    protected override void RefreshSchedules()
    {
        LoadSchedules();
    }

    /// <summary>
    /// 현재 표시 월을 기준으로 월간 달력의 6주 × 7일 날짜 구조를 다시 생성합니다.
    /// 첫 번째 날짜는 현재 월 1일이 포함된 주의 일요일부터 시작합니다.
    /// </summary>
    private void LoadMonth()
    {
        Weeks.Clear();

        var firstDayOfMonth = new DateTime(DisplayMonth.Year, DisplayMonth.Month, 1);
        var calendarStartDate = firstDayOfMonth.AddDays(-(int)firstDayOfMonth.DayOfWeek);

        for (var weekOffset = 0; weekOffset < 6; weekOffset++)
        {
            var weekStartDate = calendarStartDate.AddDays(weekOffset * 7);
            Weeks.Add(new MonthlyWeekViewModel(weekStartDate, DisplayMonth));
        }

        UpdateSelectedDateState();
    }

    /// <summary>
    /// 현재 월간 달력에 실제로 표시되는 42일 범위와 겹치는 일정만 불러와
    /// 공통 Row 구조로 다시 배치합니다.
    /// </summary>
    public void LoadSchedules()
    {
        ClearMonthlyScheduleLayout();

        if (Weeks.Count == 0)
        {
            return;
        }

        var rangeStart = Weeks[0].WeekStartDate.Date;
        var rangeEndExclusive = rangeStart.AddDays(42);
        var schedules = ScheduleService.GetByDateRange(rangeStart, rangeEndExclusive, ShowCompletedSchedules);

        LoadScheduleLayouts(schedules);
    }

    /// <summary>
    /// Drag한 일정을 지정한 월간 날짜로 이동합니다.
    /// 사용자가 잡은 일정 조각과 Drop 날짜 사이의 날짜 차이만큼
    /// 실제 일정 전체를 이동합니다.
    /// </summary>
    public void MoveScheduleByDrop(ScheduleItem schedule, DateTime displayDate, MonthlyDayViewModel targetDay)
    {
        ArgumentNullException.ThrowIfNull(schedule);
        ArgumentNullException.ThrowIfNull(targetDay);

        var dayOffset = (targetDay.Date - displayDate.Date).Days;

        if (dayOffset == 0)
        {
            SetDropTarget(null);
            return;
        }

        var newStartAt = schedule.StartAt.AddDays(dayOffset);

        try
        {
            ScheduleService.Move(schedule.Id, newStartAt);

            SelectedDate = targetDay.Date;
            UpdateSelectedDateState();
            ErrorMessage = string.Empty;

            LoadSchedules();
        }
        catch (KeyNotFoundException exception)
        {
            ErrorMessage = exception.Message;
        }
    }

    /// <summary>
    /// Drag 중 현재 Drop 대상으로 판단된 날짜 하나만 강조합니다.
    /// </summary>
    public void SetDropTarget(MonthlyDayViewModel? targetDay)
    {
        foreach (var week in Weeks)
        {
            foreach (var day in week.Days)
            {
                day.IsDropTarget = day == targetDay;
            }
        }
    }

    /// <summary>
    /// 현재 생성된 월간 달력의 일정 Row 배치를 초기화합니다.
    /// </summary>
    private void ClearMonthlyScheduleLayout()
    {
        foreach (var week in Weeks)
        {
            week.SpanningSchedules.Clear();
            week.UpdateSpanningRowCount(0);
        }
    }

    /// <summary>
    /// 현재 달력의 모든 일정을 각 주 단위 공통 Row 레이아웃으로 배치합니다.
    /// 단일 날짜 일정과 여러 날짜 일정 모두 같은 행 계산 규칙을 사용합니다.
    /// </summary>
    private void LoadScheduleLayouts(IReadOnlyList<ScheduleItem> schedules)
    {
        foreach (var week in Weeks)
        {
            LoadScheduleLayoutForWeek(week, schedules);
        }
    }

    /// <summary>
    /// 공통 계산기를 사용해 한 주의 전체 일정 배치 결과를 만들고
    /// 월간 화면용 ViewModel로 변환합니다.
    /// </summary>
    private static void LoadScheduleLayoutForWeek(MonthlyWeekViewModel week, IReadOnlyList<ScheduleItem> schedules)
    {
        var layout = ScheduleCalendarCalculator.CreateWeekLayout(schedules, week.WeekStartDate);

        foreach (var placement in layout.Placements)
        {
            week.SpanningSchedules.Add(new MonthlySpanningScheduleViewModel(
                placement.Schedule,
                placement.VisibleStartDate,
                placement.VisibleEndDate,
                placement.StartDayIndex,
                placement.DaySpan,
                placement.RowIndex,
                placement.ContinuesToNextWeek));
        }

        week.UpdateSpanningRowCount(layout.RowCount);
    }

    /// <summary>
    /// 현재 표시 중인 월을 지정한 개월 수만큼 이동합니다.
    /// </summary>
    private void MoveMonth(int monthOffset)
    {
        DisplayMonth = DisplayMonth.AddMonths(monthOffset);
        SelectedDate = DisplayMonth;

        CloseEditor();
        LoadMonth();
        LoadSchedules();
    }

    /// <summary>
    /// 오늘이 포함된 현재 달로 이동하고 오늘 날짜를 선택합니다.
    /// </summary>
    private void MoveToCurrentMonth()
    {
        DisplayMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        SelectedDate = DateTime.Today;

        CloseEditor();
        LoadMonth();
        LoadSchedules();
    }

    /// <summary>
    /// 생성된 42개의 날짜 셀 중
    /// 현재 선택 날짜와 일치하는 날짜 하나만 선택 상태로 설정합니다.
    /// </summary>
    private void UpdateSelectedDateState()
    {
        foreach (var week in Weeks)
        {
            foreach (var day in week.Days)
            {
                day.IsSelected = day.Date == SelectedDate;
            }
        }
    }

    /// <summary>
    /// 월간 달력에서 사용자가 클릭한 날짜를 현재 선택 날짜로 변경합니다.
    /// </summary>
    private void SelectDate(MonthlyDayViewModel? selectedDay)
    {
        if (selectedDay is null)
        {
            return;
        }

        SelectedDate = selectedDay.Date;
        UpdateSelectedDateState();
    }
}