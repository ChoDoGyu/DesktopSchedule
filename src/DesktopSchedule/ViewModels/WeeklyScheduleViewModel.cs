using System.Collections.ObjectModel;
using DesktopSchedule.Commands;
using DesktopSchedule.Models;
using DesktopSchedule.Services;
using DesktopSchedule.Utilities;

namespace DesktopSchedule.ViewModels;

/// <summary>
/// 주간 7일 일정 화면의 달력 상태와 사용자 동작을 관리합니다.
/// 일정 편집과 CRUD 공통 기능은 ScheduleEditorViewModelBase에서 제공하고,
/// 모든 일정의 날짜 범위와 Row 배치 계산은 ScheduleCalendarCalculator에서 제공합니다.
/// </summary>
public class WeeklyScheduleViewModel : ScheduleEditorViewModelBase
{
    // 일정 한 행이 사용하는 화면 높이입니다.
    // 공통 일정 Panel의 RowHeight와 동일하게 사용합니다.
    private const double ScheduleRowHeight = 28.0;

    // 일정 영역 아래쪽에 추가하는 여백입니다.
    private const double ScheduleAreaPadding = 4.0;

    // 현재 화면에 표시하고 있는 주의 시작 날짜입니다.
    private DateTime _weekStartDate;

    // 사용자가 현재 선택한 날짜입니다.
    private DateTime _selectedDate;

    // 일정 막대가 사용하는 전체 화면 높이입니다.
    private double _spanningAreaHeight;

    /// <summary>
    /// 현재 주의 일요일부터 토요일까지 7일입니다.
    /// </summary>
    public ObservableCollection<WeeklyDayViewModel> Days { get; } = new();

    /// <summary>
    /// 현재 주에 표시할 모든 일정입니다.
    /// 단일 날짜 일정과 여러 날짜 일정 모두 동일한 공통 Row 구조를 사용합니다.
    /// </summary>
    public ObservableCollection<WeeklySpanningScheduleViewModel> SpanningSchedules { get; } = new();

    /// <summary>
    /// 현재 주의 일정들이 차지할 전체 화면 높이입니다.
    /// </summary>
    public double SpanningAreaHeight
    {
        get => _spanningAreaHeight;
        private set => SetProperty(ref _spanningAreaHeight, value);
    }

    /// <summary>
    /// 현재 화면에 표시하는 주의 시작 날짜입니다.
    /// 일요일을 기준으로 합니다.
    /// </summary>
    public DateTime WeekStartDate
    {
        get => _weekStartDate;
        private set
        {
            if (SetProperty(ref _weekStartDate, value))
            {
                OnPropertyChanged(nameof(WeekEndDate));
                OnPropertyChanged(nameof(WeekRangeText));
            }
        }
    }

    /// <summary>
    /// 현재 표시 주의 마지막 날짜입니다.
    /// </summary>
    public DateTime WeekEndDate => WeekStartDate.AddDays(6);

    /// <summary>
    /// 화면 상단에 표시하는 현재 주의 날짜 범위입니다.
    /// </summary>
    public string WeekRangeText => $"{WeekStartDate:yyyy.MM.dd} ~ {WeekEndDate:yyyy.MM.dd}";

    /// <summary>
    /// 현재 선택된 날짜입니다.
    /// 새 일정을 만들 때 기본 날짜로 사용합니다.
    /// </summary>
    public DateTime SelectedDate
    {
        get => _selectedDate;
        private set => SetProperty(ref _selectedDate, value.Date);
    }

    /// <summary>
    /// 이전 주로 이동합니다.
    /// </summary>
    public RelayCommand PreviousWeekCommand { get; }

    /// <summary>
    /// 현재 날짜가 포함된 이번 주로 이동합니다.
    /// </summary>
    public RelayCommand CurrentWeekCommand { get; }

    /// <summary>
    /// 다음 주로 이동합니다.
    /// </summary>
    public RelayCommand NextWeekCommand { get; }

    /// <summary>
    /// 지정한 날짜만 선택합니다.
    /// </summary>
    public RelayCommand SelectDateCommand { get; }

    /// <summary>
    /// 날짜를 선택한 뒤 해당 날짜를 기준으로 새 일정 편집기를 엽니다.
    /// </summary>
    public RelayCommand OpenNewScheduleForDateCommand { get; }

    public WeeklyScheduleViewModel(ScheduleService scheduleService) : base(scheduleService)
    {
        PreviousWeekCommand = new RelayCommand(_ => MoveWeek(-1));
        CurrentWeekCommand = new RelayCommand(_ => MoveToCurrentWeek());
        NextWeekCommand = new RelayCommand(_ => MoveWeek(1));

        SelectDateCommand = new RelayCommand(parameter => SelectDate(parameter as WeeklyDayViewModel));
        OpenNewScheduleForDateCommand = new RelayCommand(parameter => OpenNewScheduleForDate(parameter as WeeklyDayViewModel));

        WeekStartDate = GetWeekStart(DateTime.Today);
        SelectedDate = DateTime.Today;

        LoadWeek();
    }

    /// <summary>
    /// 공통 일정 편집 기능이 사용할 현재 선택 날짜를 반환합니다.
    /// </summary>
    protected override DateTime GetSelectedDateForEditor()
    {
        return SelectedDate;
    }

    /// <summary>
    /// 일정 추가, 수정, 삭제, 완료 상태 변경 후
    /// 현재 주의 화면을 다시 구성합니다.
    /// </summary>
    protected override void RefreshSchedules()
    {
        LoadWeek();
    }

    /// <summary>
    /// 현재 주의 날짜 정보와 전체 일정 Row 레이아웃을 다시 구성합니다.
    /// </summary>
    private void LoadWeek()
    {
        Days.Clear();
        SpanningSchedules.Clear();

        var schedules = ScheduleService.GetAll(ShowCompletedSchedules);

        LoadScheduleLayout(schedules);
        LoadDays();
    }

    /// <summary>
    /// 공통 계산기를 사용해 현재 주에서 보이는 모든 일정의
    /// 날짜 범위와 겹침 Row를 계산한 뒤 주간 화면용 ViewModel로 변환합니다.
    /// </summary>
    private void LoadScheduleLayout(IReadOnlyList<ScheduleItem> schedules)
    {
        var layout = ScheduleCalendarCalculator.CreateWeekLayout(schedules, WeekStartDate);

        foreach (var placement in layout.Placements)
        {
            SpanningSchedules.Add(
                new WeeklySpanningScheduleViewModel(
                    placement.Schedule,
                    placement.VisibleStartDate,
                    placement.StartDayIndex,
                    placement.DaySpan,
                    placement.RowIndex));
        }

        SpanningAreaHeight = layout.RowCount == 0
            ? 0
            : layout.RowCount * ScheduleRowHeight + ScheduleAreaPadding;
    }

    /// <summary>
    /// 현재 주의 일요일부터 토요일까지 날짜 정보를 생성합니다.
    /// 일정 자체는 공통 Row 레이아웃에서 별도로 관리합니다.
    /// </summary>
    private void LoadDays()
    {
        for (var dayOffset = 0; dayOffset < 7; dayOffset++)
        {
            var date = WeekStartDate.AddDays(dayOffset);

            var day = new WeeklyDayViewModel(date)
            {
                IsSelected = date.Date == SelectedDate.Date
            };

            Days.Add(day);
        }
    }

    /// <summary>
    /// Drag한 일정을 지정한 날짜로 이동합니다.
    /// 사용자가 잡은 화면상의 날짜를 기준으로 일정 전체를 같은 일수만큼 이동합니다.
    /// </summary>
    public void MoveScheduleByDrop(ScheduleItem schedule, DateTime displayDate, WeeklyDayViewModel targetDay)
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
            ErrorMessage = string.Empty;

            LoadWeek();
        }
        catch (KeyNotFoundException exception)
        {
            ErrorMessage = exception.Message;
        }
    }

    /// <summary>
    /// Drag 중 현재 Drop 대상인 날짜 하나만 강조합니다.
    /// </summary>
    public void SetDropTarget(WeeklyDayViewModel? targetDay)
    {
        foreach (var day in Days)
        {
            day.IsDropTarget = day == targetDay;
        }
    }

    /// <summary>
    /// 지정한 날짜를 현재 선택 날짜로 변경합니다.
    /// </summary>
    private void SelectDate(WeeklyDayViewModel? selectedDay)
    {
        if (selectedDay is null)
        {
            return;
        }

        SelectedDate = selectedDay.Date;

        foreach (var day in Days)
        {
            day.IsSelected = day.Date == SelectedDate;
        }
    }

    /// <summary>
    /// 날짜를 선택한 뒤 해당 날짜를 기본값으로 새 일정 편집기를 엽니다.
    /// </summary>
    private void OpenNewScheduleForDate(WeeklyDayViewModel? selectedDay)
    {
        if (selectedDay is null)
        {
            return;
        }

        SelectDate(selectedDay);
        PrepareNewSchedule();
    }

    /// <summary>
    /// 현재 표시 중인 주를 지정한 주 수만큼 이동합니다.
    /// </summary>
    private void MoveWeek(int weekOffset)
    {
        WeekStartDate = WeekStartDate.AddDays(7 * weekOffset);
        SelectedDate = WeekStartDate;

        CloseEditor();
        LoadWeek();
    }

    /// <summary>
    /// 오늘이 포함된 현재 주로 이동합니다.
    /// </summary>
    private void MoveToCurrentWeek()
    {
        WeekStartDate = GetWeekStart(DateTime.Today);
        SelectedDate = DateTime.Today;

        CloseEditor();
        LoadWeek();
    }

    /// <summary>
    /// 전달된 날짜가 포함된 주의 일요일을 반환합니다.
    /// </summary>
    private static DateTime GetWeekStart(DateTime date)
    {
        return date.Date.AddDays(-(int)date.DayOfWeek);
    }
}