using System.Collections.ObjectModel;
using DesktopSchedule.Commands;
using DesktopSchedule.Models;
using DesktopSchedule.Services;

namespace DesktopSchedule.ViewModels;

/// <summary>
/// 주간 7일 일정 화면의 달력 상태와 사용자 동작을 관리합니다.
/// 일정 편집과 CRUD 공통 기능은 ScheduleEditorViewModelBase에서 제공합니다.
/// </summary>
public class WeeklyScheduleViewModel : ScheduleEditorViewModelBase
{
    // 여러 날짜 일정 한 행이 사용하는 화면 높이입니다.
    private const double SpanningScheduleRowHeight = 31.0;

    // 여러 날짜 일정 영역 아래쪽에 추가하는 여백입니다.
    private const double SpanningScheduleAreaPadding = 4.0;

    // 현재 화면에 표시하고 있는 주의 시작 날짜입니다.
    private DateTime _weekStartDate;

    // 사용자가 현재 선택한 날짜입니다.
    private DateTime _selectedDate;

    // 여러 날짜 일정 막대가 사용하는 전체 높이입니다.
    private double _spanningAreaHeight;

    /// <summary>
    /// 현재 주의 일요일부터 토요일까지 7일입니다.
    /// </summary>
    public ObservableCollection<WeeklyDayViewModel> Days { get; } = new();

    /// <summary>
    /// 현재 주에서 여러 날짜에 걸쳐 연결해서 표시할 일정 막대입니다.
    /// </summary>
    public ObservableCollection<WeeklySpanningScheduleViewModel> SpanningSchedules { get; } = new();

    /// <summary>
    /// 여러 날짜 일정 막대가 차지할 화면 높이입니다.
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
    /// 현재 주의 날짜별 일정과 여러 날짜 연결 일정을 다시 구성합니다.
    /// </summary>
    private void LoadWeek()
    {
        Days.Clear();
        SpanningSchedules.Clear();

        var schedules = ScheduleService.GetAll(ShowCompletedSchedules);

        LoadSpanningSchedules(schedules);
        LoadDays(schedules);
    }

    /// <summary>
    /// 두 날짜 이상에 걸친 일정을 하나의 연결된 막대로 구성합니다.
    /// 일정이 현재 주의 바깥까지 이어지면 현재 주에 보이는 부분만 잘라 표시합니다.
    /// </summary>
    private void LoadSpanningSchedules(IReadOnlyList<ScheduleItem> schedules)
    {
        var candidates = new List<SpanningScheduleCandidate>();

        foreach (var schedule in schedules)
        {
            if (!IsSpanningSchedule(schedule))
            {
                continue;
            }

            var scheduleStartDate = schedule.StartAt.Date;
            var scheduleEndDate = GetLastDisplayDate(schedule);

            var visibleStartDate = scheduleStartDate < WeekStartDate ? WeekStartDate : scheduleStartDate;
            var visibleEndDate = scheduleEndDate > WeekEndDate ? WeekEndDate : scheduleEndDate;

            if (visibleStartDate > visibleEndDate)
            {
                continue;
            }

            var startDayIndex = (visibleStartDate - WeekStartDate).Days;
            var endDayIndex = (visibleEndDate - WeekStartDate).Days;

            candidates.Add(new SpanningScheduleCandidate(
                schedule,
                visibleStartDate,
                startDayIndex,
                endDayIndex));
        }

        var orderedCandidates = candidates
            .OrderBy(candidate => candidate.StartDayIndex)
            .ThenByDescending(candidate => candidate.EndDayIndex)
            .ThenBy(candidate => candidate.Schedule.StartAt)
            .ToList();

        var rowEndDayIndices = new List<int>();

        foreach (var candidate in orderedCandidates)
        {
            var rowIndex = FindAvailableSpanningRow(rowEndDayIndices, candidate.StartDayIndex);

            if (rowIndex == rowEndDayIndices.Count)
            {
                rowEndDayIndices.Add(candidate.EndDayIndex);
            }
            else
            {
                rowEndDayIndices[rowIndex] = candidate.EndDayIndex;
            }

            var daySpan = candidate.EndDayIndex - candidate.StartDayIndex + 1;

            SpanningSchedules.Add(
                new WeeklySpanningScheduleViewModel(
                    candidate.Schedule,
                    candidate.VisibleStartDate,
                    candidate.StartDayIndex,
                    daySpan,
                    rowIndex));
        }

        SpanningAreaHeight = rowEndDayIndices.Count == 0
            ? 0
            : rowEndDayIndices.Count * SpanningScheduleRowHeight + SpanningScheduleAreaPadding;
    }

    /// <summary>
    /// 여러 날짜 일정 막대가 사용할 수 있는 가장 위쪽의 빈 행을 찾습니다.
    /// </summary>
    private static int FindAvailableSpanningRow(List<int> rowEndDayIndices, int startDayIndex)
    {
        for (var index = 0; index < rowEndDayIndices.Count; index++)
        {
            if (startDayIndex > rowEndDayIndices[index])
            {
                return index;
            }
        }

        return rowEndDayIndices.Count;
    }

    /// <summary>
    /// 각 날짜 칸에 표시할 단일 날짜 일정을 구성합니다.
    /// 여러 날짜 일정은 위쪽 연결 막대에 표시하므로 여기서는 제외합니다.
    /// </summary>
    private void LoadDays(IReadOnlyList<ScheduleItem> schedules)
    {
        for (var dayOffset = 0; dayOffset < 7; dayOffset++)
        {
            var date = WeekStartDate.AddDays(dayOffset);

            var day = new WeeklyDayViewModel(date)
            {
                IsSelected = date.Date == SelectedDate.Date
            };

            var schedulesOnDate = schedules
                .Where(schedule => !IsSpanningSchedule(schedule) && IsScheduleOnDate(schedule, date))
                .OrderBy(schedule => schedule.IsAllDay ? 0 : 1)
                .ThenBy(schedule => schedule.StartAt)
                .ThenBy(schedule => schedule.Title)
                .ToList();

            foreach (var schedule in schedulesOnDate)
            {
                day.Schedules.Add(new WeeklyScheduleCardViewModel(schedule, date));
            }

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
    /// 일정이 두 개 이상의 실제 날짜 칸을 차지하는지 확인합니다.
    /// </summary>
    private static bool IsSpanningSchedule(ScheduleItem schedule)
    {
        return GetLastDisplayDate(schedule) > schedule.StartAt.Date;
    }

    /// <summary>
    /// 일정이 화면에서 실제로 마지막으로 차지하는 날짜를 반환합니다.
    /// 시간 일정이 정확히 다음 날 00:00에 끝나면 그 다음 날짜는 차지하지 않습니다.
    /// 하루 종일 일정의 종료 날짜는 포함해서 표시합니다.
    /// </summary>
    private static DateTime GetLastDisplayDate(ScheduleItem schedule)
    {
        if (schedule.IsAllDay)
        {
            return schedule.EndAt.Date;
        }

        if (schedule.EndAt > schedule.StartAt &&
            schedule.EndAt.TimeOfDay == TimeSpan.Zero)
        {
            return schedule.EndAt.Date.AddDays(-1);
        }

        return schedule.EndAt.Date;
    }

    /// <summary>
    /// 지정한 일정이 현재 날짜에 포함되는지 확인합니다.
    /// </summary>
    private static bool IsScheduleOnDate(ScheduleItem schedule, DateTime date)
    {
        if (schedule.IsAllDay)
        {
            return schedule.StartAt.Date <= date.Date &&
                   schedule.EndAt.Date >= date.Date;
        }

        var dayStart = date.Date;
        var dayEnd = dayStart.AddDays(1);

        return schedule.StartAt < dayEnd &&
               schedule.EndAt > dayStart;
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

    /// <summary>
    /// 여러 날짜 일정의 주간 표시 계산에 사용하는 내부 데이터입니다.
    /// </summary>
    private readonly record struct SpanningScheduleCandidate(
        ScheduleItem Schedule,
        DateTime VisibleStartDate,
        int StartDayIndex,
        int EndDayIndex);
}