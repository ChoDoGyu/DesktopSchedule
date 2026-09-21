using System.Collections.ObjectModel; // WPF에서 목록 변경을 감지할 수 있는 ObservableCollection을 사용하기 위해 필요합니다.
using DesktopSchedule.Commands; // RelayCommand를 사용하기 위해 필요합니다.
using DesktopSchedule.Models; // ScheduleItem을 사용하기 위해 필요합니다.
using DesktopSchedule.Services; // ScheduleService를 사용하기 위해 필요합니다.

namespace DesktopSchedule.ViewModels;

/// <summary>
/// 월간 달력 화면의 상태와 일정 편집 동작을 관리합니다.
/// 현재 표시 월의 6주 × 7일 날짜 구조와 일정 CRUD 상태를 함께 관리합니다.
/// </summary>
public class MonthlyCalendarViewModel : ViewModelBase
{
    // 일정 조회, 생성, 수정, 삭제, 이동 기능을 제공하는 Service입니다.
    private readonly ScheduleService _scheduleService;

    // 현재 월간 달력에서 표시하고 있는 월입니다.
    // 날짜는 항상 해당 월의 1일로 정규화해서 관리합니다.
    private DateTime _displayMonth;

    // 사용자가 현재 선택한 날짜입니다.
    private DateTime _selectedDate;

    // 현재 편집 중인 기존 일정의 Id입니다.
    // null이면 새 일정을 작성하는 상태입니다.
    private Guid? _editingScheduleId;

    // 현재 편집 중인 일정의 완료 상태입니다.
    private bool _isEditingCompleted;

    // 일정 편집기를 화면에 표시할지 여부입니다.
    private bool _isEditorOpen;

    // 완료된 일정도 목록에 포함해서 표시할지 여부입니다.
    private bool _showCompletedSchedules;

    // 일정 제목 입력값입니다.
    private string _newTitle = string.Empty;

    // 일정 설명 입력값입니다.
    private string _newDescription = string.Empty;

    // 일정 시작 날짜입니다.
    private DateTime? _newStartDate = DateTime.Today;

    // 일정 시작 시간 문자열입니다.
    private string _newStartTime = "09:00";

    // 일정 종료 날짜입니다.
    private DateTime? _newEndDate = DateTime.Today;

    // 일정 종료 시간 문자열입니다.
    private string _newEndTime = "10:00";

    // 하루 종일 일정인지 여부입니다.
    private bool _newIsAllDay;

    // 일정 알림을 사용할지 여부입니다.
    private bool _newIsReminderEnabled;

    // 일정 시작 몇 분 전에 알림을 발생시킬지 저장합니다.
    private int _newReminderMinutesBefore = 10;

    // 일정 입력 과정에서 발생한 오류 메시지입니다.
    private string _errorMessage = string.Empty;

    /// <summary>
    /// 현재 월간 달력에 표시되는 6개의 주입니다.
    /// 각 주는 일요일부터 토요일까지 7개의 날짜 셀을 가집니다.
    /// </summary>
    public ObservableCollection<MonthlyWeekViewModel> Weeks { get; } = new();

    /// <summary>
    /// 현재 화면에서 사용하는 전체 일정 목록입니다.
    /// 날짜 셀과 여러 날짜 연결 일정도 모두 같은 ScheduleItem 데이터를 사용합니다.
    /// </summary>
    public ObservableCollection<ScheduleItem> Schedules { get; } = new();

    /// <summary>
    /// 일정 알림에서 선택할 수 있는 시작 전 시간 목록입니다.
    /// </summary>
    public IReadOnlyList<int> ReminderMinuteOptions { get; } = new[] { 0, 5, 10, 30, 60, 1440 };

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
    /// 예: "2026년 9월"
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
    /// 현재 편집 중인 기존 일정의 Id입니다.
    /// null이면 새 일정을 작성하는 상태입니다.
    /// </summary>
    public Guid? EditingScheduleId
    {
        get => _editingScheduleId;
        private set
        {
            if (SetProperty(ref _editingScheduleId, value))
            {
                OnPropertyChanged(nameof(IsEditMode));
                OnPropertyChanged(nameof(EditorTitle));
            }
        }
    }

    /// <summary>
    /// 기존 일정을 수정 중인지 여부입니다.
    /// </summary>
    public bool IsEditMode => EditingScheduleId.HasValue;

    /// <summary>
    /// 현재 편집 중인 일정이 완료 상태인지 여부입니다.
    /// </summary>
    public bool IsEditingCompleted
    {
        get => _isEditingCompleted;
        private set => SetProperty(ref _isEditingCompleted, value);
    }

    /// <summary>
    /// 현재 편집 모드에 따라 화면에 표시할 제목입니다.
    /// </summary>
    public string EditorTitle => IsEditMode ? "일정 수정" : "새 일정";

    /// <summary>
    /// 일정 편집기를 화면에 표시할지 여부입니다.
    /// </summary>
    public bool IsEditorOpen
    {
        get => _isEditorOpen;
        set => SetProperty(ref _isEditorOpen, value);
    }

    /// <summary>
    /// 완료된 일정까지 목록에 표시할지 여부입니다.
    /// 값이 변경되면 전체 일정과 월간 날짜별 일정을 다시 구성합니다.
    /// </summary>
    public bool ShowCompletedSchedules
    {
        get => _showCompletedSchedules;
        set
        {
            if (SetProperty(ref _showCompletedSchedules, value))
            {
                LoadSchedules();
            }
        }
    }

    /// <summary>
    /// 일정 제목 입력값입니다.
    /// </summary>
    public string NewTitle
    {
        get => _newTitle;
        set => SetProperty(ref _newTitle, value);
    }

    /// <summary>
    /// 일정 설명 입력값입니다.
    /// </summary>
    public string NewDescription
    {
        get => _newDescription;
        set => SetProperty(ref _newDescription, value);
    }

    /// <summary>
    /// 일정 시작 날짜입니다.
    /// </summary>
    public DateTime? NewStartDate
    {
        get => _newStartDate;
        set => SetProperty(ref _newStartDate, value);
    }

    /// <summary>
    /// 일정 시작 시간입니다.
    /// </summary>
    public string NewStartTime
    {
        get => _newStartTime;
        set => SetProperty(ref _newStartTime, value);
    }

    /// <summary>
    /// 일정 종료 날짜입니다.
    /// </summary>
    public DateTime? NewEndDate
    {
        get => _newEndDate;
        set => SetProperty(ref _newEndDate, value);
    }

    /// <summary>
    /// 일정 종료 시간입니다.
    /// </summary>
    public string NewEndTime
    {
        get => _newEndTime;
        set => SetProperty(ref _newEndTime, value);
    }

    /// <summary>
    /// 하루 종일 일정인지 여부입니다.
    /// </summary>
    public bool NewIsAllDay
    {
        get => _newIsAllDay;
        set => SetProperty(ref _newIsAllDay, value);
    }

    /// <summary>
    /// 현재 일정에서 알림 기능을 사용할지 여부입니다.
    /// </summary>
    public bool NewIsReminderEnabled
    {
        get => _newIsReminderEnabled;
        set => SetProperty(ref _newIsReminderEnabled, value);
    }

    /// <summary>
    /// 일정 시작 몇 분 전에 알림을 발생시킬지 나타냅니다.
    /// </summary>
    public int NewReminderMinutesBefore
    {
        get => _newReminderMinutesBefore;
        set => SetProperty(ref _newReminderMinutesBefore, value);
    }

    /// <summary>
    /// 일정 입력 과정에서 사용자에게 표시할 오류 메시지입니다.
    /// </summary>
    public string ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    /// <summary>
    /// 이전 달로 이동하는 Command입니다.
    /// </summary>
    public RelayCommand PreviousMonthCommand { get; }

    /// <summary>
    /// 오늘이 포함된 현재 달로 돌아가는 Command입니다.
    /// </summary>
    public RelayCommand CurrentMonthCommand { get; }

    /// <summary>
    /// 다음 달로 이동하는 Command입니다.
    /// </summary>
    public RelayCommand NextMonthCommand { get; }

    /// <summary>
    /// 월간 달력에서 선택한 날짜를 현재 선택 날짜로 변경하는 Command입니다.
    /// </summary>
    public RelayCommand SelectDateCommand { get; }

    /// <summary>
    /// 새 일정 작성 상태로 편집기를 여는 Command입니다.
    /// </summary>
    public RelayCommand OpenNewScheduleCommand { get; }

    /// <summary>
    /// 선택한 기존 일정을 편집 상태로 여는 Command입니다.
    /// </summary>
    public RelayCommand SelectScheduleCommand { get; }

    /// <summary>
    /// 현재 입력값을 새 일정으로 추가하거나 기존 일정에 저장하는 Command입니다.
    /// </summary>
    public RelayCommand SaveScheduleCommand { get; }

    /// <summary>
    /// 현재 편집 중인 일정을 영구적으로 삭제하는 Command입니다.
    /// </summary>
    public RelayCommand DeleteScheduleCommand { get; }

    /// <summary>
    /// 현재 편집 중인 일정을 완료 상태로 변경하는 Command입니다.
    /// </summary>
    public RelayCommand MarkAsCompletedCommand { get; }

    /// <summary>
    /// 현재 편집 중인 일정의 완료 상태를 취소하는 Command입니다.
    /// </summary>
    public RelayCommand MarkAsIncompleteCommand { get; }

    /// <summary>
    /// 현재 일정 편집을 취소하는 Command입니다.
    /// </summary>
    public RelayCommand CancelEditCommand { get; }

    public MonthlyCalendarViewModel(ScheduleService scheduleService)
    {
        _scheduleService = scheduleService ?? throw new ArgumentNullException(nameof(scheduleService));

        PreviousMonthCommand = new RelayCommand(_ => MoveMonth(-1));
        CurrentMonthCommand = new RelayCommand(_ => MoveToCurrentMonth());
        NextMonthCommand = new RelayCommand(_ => MoveMonth(1));
        SelectDateCommand = new RelayCommand(parameter => SelectDate(parameter as MonthlyDayViewModel));

        OpenNewScheduleCommand = new RelayCommand(_ => PrepareNewSchedule());
        SelectScheduleCommand = new RelayCommand(parameter => SelectSchedule(parameter as ScheduleItem));
        SaveScheduleCommand = new RelayCommand(_ => SaveSchedule());
        DeleteScheduleCommand = new RelayCommand(_ => DeleteSchedule(), _ => IsEditMode);
        MarkAsCompletedCommand = new RelayCommand(_ => MarkAsCompleted(), _ => IsEditMode && !IsEditingCompleted);
        MarkAsIncompleteCommand = new RelayCommand(_ => MarkAsIncomplete(), _ => IsEditMode && IsEditingCompleted);
        CancelEditCommand = new RelayCommand(_ => CloseEditor());

        DisplayMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        SelectedDate = DateTime.Today;

        LoadMonth();
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

        // DayOfWeek에서 Sunday는 0이므로
        // 해당 월 1일의 요일 값만큼 뒤로 이동하면 달력의 첫 일요일을 얻을 수 있습니다.
        var calendarStartDate = firstDayOfMonth.AddDays(-(int)firstDayOfMonth.DayOfWeek);

        for (var weekOffset = 0; weekOffset < 6; weekOffset++)
        {
            var weekStartDate = calendarStartDate.AddDays(weekOffset * 7);
            Weeks.Add(new MonthlyWeekViewModel(weekStartDate, DisplayMonth));
        }

        UpdateSelectedDateState();
    }

    /// <summary>
    /// 저장된 일정을 다시 불러옵니다.
    /// 전체 일정 목록과 현재 월간 달력의 일정 표시 데이터를 함께 갱신합니다.
    /// </summary>
    public void LoadSchedules()
    {
        Schedules.Clear();
        ClearMonthlyScheduleLayout();

        var schedules = _scheduleService.GetAll(ShowCompletedSchedules);

        foreach (var schedule in schedules)
        {
            Schedules.Add(schedule);
        }

        LoadSingleDaySchedules(schedules);
        LoadSpanningSchedules(schedules);
    }

    /// <summary>
    /// Drag한 일정을 지정한 월간 날짜로 이동합니다.
    /// 사용자가 실제로 잡은 화면상의 날짜와 Drop 날짜의 차이를 계산한 뒤,
    /// 일정 전체를 같은 일수만큼 이동합니다.
    /// </summary>
    public void MoveScheduleByDrop(ScheduleItem schedule, DateTime displayDate, MonthlyDayViewModel targetDay)
    {
        ArgumentNullException.ThrowIfNull(schedule);
        ArgumentNullException.ThrowIfNull(targetDay);

        // 사용자가 잡은 일정 조각의 날짜와 Drop한 날짜 사이의 차이를 계산합니다.
        // 여러 날짜 일정의 중간 주에 표시된 조각을 잡더라도
        // 그 조각을 기준으로 전체 일정이 자연스럽게 같은 일수만큼 이동합니다.
        var dayOffset = (targetDay.Date - displayDate.Date).Days;

        // 같은 날짜에 다시 놓은 경우 DB를 불필요하게 갱신하지 않습니다.
        if (dayOffset == 0)
        {
            SetDropTarget(null);
            return;
        }

        // 기존 일정의 시간은 그대로 유지하고 날짜만 같은 일수만큼 이동합니다.
        // 실제 EndAt 이동은 ScheduleService.Move()가 기존 duration을 이용해 처리합니다.
        var newStartAt = schedule.StartAt.AddDays(dayOffset);

        try
        {
            _scheduleService.Move(schedule.Id, newStartAt);

            // Drop한 날짜를 새로운 선택 날짜로 사용합니다.
            SelectedDate = targetDay.Date;
            UpdateSelectedDateState();

            ErrorMessage = string.Empty;

            // 이동된 일정이 단일 일정인지 여러 날짜 일정인지 다시 판단해야 하므로
            // 월간 일정 배치 전체를 다시 구성합니다.
            LoadSchedules();
        }
        catch (KeyNotFoundException exception)
        {
            ErrorMessage = exception.Message;
        }
    }

    /// <summary>
    /// Drag 중 현재 Drop 대상으로 판단된 날짜 하나만 강조합니다.
    /// 주간 화면의 SetDropTarget과 동일하게 모든 날짜를 순회하면서
    /// 전달된 날짜만 IsDropTarget 상태로 설정합니다.
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
    /// 현재 생성된 월간 달력의 일정 표시 데이터를 초기화합니다.
    /// 단일 날짜 일정과 여러 날짜 연결 일정 모두 제거하여
    /// 이후 일정 데이터를 처음부터 다시 구성할 수 있도록 합니다.
    /// </summary>
    private void ClearMonthlyScheduleLayout()
    {
        foreach (var week in Weeks)
        {
            week.SpanningSchedules.Clear();
            week.UpdateSpanningRowCount(0);

            foreach (var day in week.Days)
            {
                day.Schedules.Clear();
            }
        }
    }

    /// <summary>
    /// 현재 42일 달력 범위에 포함되는 단일 날짜 일정을 날짜별로 배치합니다.
    /// 여러 날짜에 걸치는 일정은 연결 막대로 처리하므로 여기서는 제외합니다.
    /// </summary>
    private void LoadSingleDaySchedules(IReadOnlyList<ScheduleItem> schedules)
    {
        foreach (var week in Weeks)
        {
            foreach (var day in week.Days)
            {
                var schedulesOnDate = schedules
                    .Where(schedule => !IsSpanningSchedule(schedule) && IsScheduleOnDate(schedule, day.Date))
                    .OrderBy(schedule => schedule.IsAllDay ? 0 : 1)
                    .ThenBy(schedule => schedule.StartAt)
                    .ThenBy(schedule => schedule.Title)
                    .ToList();

                foreach (var schedule in schedulesOnDate)
                {
                    day.Schedules.Add(new MonthlyScheduleCardViewModel(schedule, day.Date));
                }
            }
        }
    }

    /// <summary>
    /// 여러 날짜에 걸친 일정을 각 주 안에서 보이는 구간으로 분할하고,
    /// 같은 주에서 서로 겹치지 않도록 세로 행을 계산하여 배치합니다.
    /// </summary>
    private void LoadSpanningSchedules(IReadOnlyList<ScheduleItem> schedules)
    {
        foreach (var week in Weeks)
        {
            LoadSpanningSchedulesForWeek(week, schedules);
        }
    }

    /// <summary>
    /// 지정한 한 주에 보이는 여러 날짜 일정들을 수집한 뒤
    /// 일정이 겹치지 않는 가장 위쪽 행부터 차례대로 배치합니다.
    /// </summary>
    private static void LoadSpanningSchedulesForWeek(MonthlyWeekViewModel week, IReadOnlyList<ScheduleItem> schedules)
    {
        var candidates = new List<MonthlySpanningScheduleCandidate>();

        foreach (var schedule in schedules)
        {
            if (!IsSpanningSchedule(schedule))
            {
                continue;
            }

            var scheduleStartDate = schedule.StartAt.Date;
            var scheduleEndDate = GetLastDisplayDate(schedule);

            var visibleStartDate = scheduleStartDate < week.WeekStartDate
                ? week.WeekStartDate
                : scheduleStartDate;

            var visibleEndDate = scheduleEndDate > week.WeekEndDate
                ? week.WeekEndDate
                : scheduleEndDate;

            if (visibleStartDate > visibleEndDate)
            {
                continue;
            }

            var startDayIndex = (visibleStartDate - week.WeekStartDate).Days;
            var endDayIndex = (visibleEndDate - week.WeekStartDate).Days;

            candidates.Add(
                new MonthlySpanningScheduleCandidate(
                    schedule,
                    visibleStartDate,
                    visibleEndDate,
                    startDayIndex,
                    endDayIndex,
                    scheduleEndDate > visibleEndDate));
        }

        // 같은 날짜에 여러 일정이 시작하면 더 길게 이어지는 일정을 먼저 배치합니다.
        // 이렇게 하면 긴 일정이 위쪽 행에 안정적으로 자리 잡아
        // 월간 달력에서 연결 흐름을 읽기 쉬워집니다.
        var orderedCandidates = candidates
            .OrderBy(candidate => candidate.StartDayIndex)
            .ThenByDescending(candidate => candidate.EndDayIndex)
            .ThenBy(candidate => candidate.Schedule.StartAt)
            .ThenBy(candidate => candidate.Schedule.Title)
            .ToList();

        // 각 행에서 현재 가장 마지막으로 사용 중인 날짜 열 번호를 저장합니다.
        // 예를 들어 첫 번째 행이 목요일까지 사용 중이라면 값은 4입니다.
        var rowEndDayIndices = new List<int>();

        foreach (var candidate in orderedCandidates)
        {
            var rowIndex = FindAvailableSpanningRow(rowEndDayIndices, candidate.StartDayIndex);

            if (rowIndex == rowEndDayIndices.Count)
            {
                // 사용할 수 있는 기존 행이 없으므로 새로운 행을 추가합니다.
                rowEndDayIndices.Add(candidate.EndDayIndex);
            }
            else
            {
                // 기존 행의 빈 영역을 재사용하고
                // 이 일정이 새로 차지하는 마지막 열 번호로 갱신합니다.
                rowEndDayIndices[rowIndex] = candidate.EndDayIndex;
            }

            var daySpan = candidate.EndDayIndex - candidate.StartDayIndex + 1;

            week.SpanningSchedules.Add(
                new MonthlySpanningScheduleViewModel(
                    candidate.Schedule,
                    candidate.VisibleStartDate,
                    candidate.VisibleEndDate,
                    candidate.StartDayIndex,
                    daySpan,
                    rowIndex,
                    candidate.ContinuesToNextWeek));
        }

        week.UpdateSpanningRowCount(rowEndDayIndices.Count);
    }

    /// <summary>
    /// 연결 일정이 사용할 수 있는 가장 위쪽의 빈 행을 찾습니다.
    /// 일정의 시작 열이 기존 행의 마지막 사용 열보다 뒤에 있으면
    /// 두 일정은 서로 겹치지 않으므로 같은 행을 재사용할 수 있습니다.
    /// </summary>
    private static int FindAvailableSpanningRow(List<int> rowEndDayIndices, int startDayIndex)
    {
        for (var rowIndex = 0; rowIndex < rowEndDayIndices.Count; rowIndex++)
        {
            if (startDayIndex > rowEndDayIndices[rowIndex])
            {
                return rowIndex;
            }
        }

        return rowEndDayIndices.Count;
    }

    /// <summary>
    /// 일정이 월간 달력에서 두 개 이상의 날짜 셀을 차지하는지 확인합니다.
    /// 실제 마지막 표시 날짜가 시작 날짜보다 뒤라면 연결 일정으로 판단합니다.
    /// </summary>
    private static bool IsSpanningSchedule(ScheduleItem schedule)
    {
        return GetLastDisplayDate(schedule) > schedule.StartAt.Date;
    }

    /// <summary>
    /// 일정이 월간 달력에서 실제로 마지막으로 차지해야 하는 날짜를 반환합니다.
    /// 시간 일정이 정확히 다음 날 00:00에 끝나면 그 날짜는 표시하지 않고,
    /// 하루 종일 일정은 종료 날짜까지 포함합니다.
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
    /// 지정한 일정이 특정 날짜에 실제로 포함되는지 확인합니다.
    /// 하루 종일 일정은 시작일과 종료일을 모두 포함하고,
    /// 시간 일정은 해당 날짜의 24시간 범위와 겹치는지 검사합니다.
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
    /// 현재 표시 중인 월을 지정한 개월 수만큼 이동합니다.
    /// 이전 달은 -1, 다음 달은 1을 전달합니다.
    /// 이동 후에는 해당 월의 1일을 선택 날짜로 사용합니다.
    /// </summary>
    private void MoveMonth(int monthOffset)
    {
        DisplayMonth = DisplayMonth.AddMonths(monthOffset);
        SelectedDate = DisplayMonth;

        IsEditorOpen = false;

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

        IsEditorOpen = false;

        LoadMonth();
        LoadSchedules();
    }

    /// <summary>
    /// 생성된 42개의 날짜 셀 중 현재 선택 날짜와 일치하는 셀만 선택 상태로 표시합니다.
    /// 선택 날짜가 현재 42일 범위 밖에 있으면 모든 날짜가 선택 해제 상태가 됩니다.
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
    /// 42개의 날짜 셀 전체에서 선택 상태를 다시 계산하여
    /// 한 번에 하나의 날짜만 선택 상태가 되도록 유지합니다.
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

    /// <summary>
    /// 선택한 일정을 편집 상태로 엽니다.
    /// </summary>
    private void SelectSchedule(ScheduleItem? schedule)
    {
        if (schedule is null)
        {
            return;
        }

        PrepareExistingSchedule(schedule);
    }

    /// <summary>
    /// 현재 입력된 값을 새 일정으로 추가하거나 기존 일정에 저장합니다.
    /// </summary>
    private void SaveSchedule()
    {
        ErrorMessage = string.Empty;

        if (NewStartDate is null || NewEndDate is null)
        {
            ErrorMessage = "시작 날짜와 종료 날짜를 선택해주세요.";
            return;
        }

        if (NewIsAllDay)
        {
            SaveSchedule(NewStartDate.Value.Date, NewEndDate.Value.Date);
            return;
        }

        if (!TimeSpan.TryParse(NewStartTime, out var startTime))
        {
            ErrorMessage = "시작 시간을 올바르게 입력해주세요. 예: 09:30";
            return;
        }

        if (!TimeSpan.TryParse(NewEndTime, out var endTime))
        {
            ErrorMessage = "종료 시간을 올바르게 입력해주세요. 예: 10:30";
            return;
        }

        var startAt = NewStartDate.Value.Date.Add(startTime);
        var endAt = NewEndDate.Value.Date.Add(endTime);

        SaveSchedule(startAt, endAt);
    }

    /// <summary>
    /// 완성된 시작/종료 시간을 이용해 새 일정을 추가하거나 기존 일정에 저장합니다.
    /// </summary>
    private void SaveSchedule(DateTime startAt, DateTime endAt)
    {
        try
        {
            if (EditingScheduleId.HasValue)
            {
                _scheduleService.Update(EditingScheduleId.Value, NewTitle, NewDescription, startAt, endAt, NewIsAllDay, NewIsReminderEnabled, NewReminderMinutesBefore);
            }
            else
            {
                _scheduleService.Add(NewTitle, NewDescription, startAt, endAt, NewIsAllDay, NewIsReminderEnabled, NewReminderMinutesBefore);
            }

            LoadSchedules();
            CloseEditor();
        }
        catch (ArgumentException exception)
        {
            ErrorMessage = exception.Message;
        }
        catch (KeyNotFoundException exception)
        {
            ErrorMessage = exception.Message;
        }
    }

    /// <summary>
    /// 현재 편집 중인 일정을 영구적으로 삭제합니다.
    /// </summary>
    private void DeleteSchedule()
    {
        if (!EditingScheduleId.HasValue)
        {
            return;
        }

        try
        {
            _scheduleService.Delete(EditingScheduleId.Value);
            LoadSchedules();
            CloseEditor();
        }
        catch (KeyNotFoundException exception)
        {
            ErrorMessage = exception.Message;
        }
    }

    /// <summary>
    /// 현재 편집 중인 일정을 완료 상태로 변경합니다.
    /// </summary>
    private void MarkAsCompleted()
    {
        if (!EditingScheduleId.HasValue)
        {
            return;
        }

        try
        {
            _scheduleService.MarkAsCompleted(EditingScheduleId.Value);
            LoadSchedules();
            CloseEditor();
        }
        catch (KeyNotFoundException exception)
        {
            ErrorMessage = exception.Message;
        }
    }

    /// <summary>
    /// 현재 편집 중인 일정의 완료 상태를 취소합니다.
    /// </summary>
    private void MarkAsIncomplete()
    {
        if (!EditingScheduleId.HasValue)
        {
            return;
        }

        try
        {
            _scheduleService.MarkAsIncomplete(EditingScheduleId.Value);
            LoadSchedules();
            CloseEditor();
        }
        catch (KeyNotFoundException exception)
        {
            ErrorMessage = exception.Message;
        }
    }

    /// <summary>
    /// 새 일정 작성 상태로 편집기를 준비합니다.
    /// 현재 선택 날짜를 새 일정의 기본 시작/종료 날짜로 사용합니다.
    /// </summary>
    private void PrepareNewSchedule()
    {
        EditingScheduleId = null;
        IsEditingCompleted = false;

        ResetInput();

        IsEditorOpen = true;
    }

    /// <summary>
    /// 기존 일정의 값을 편집 입력 상태에 복사합니다.
    /// </summary>
    private void PrepareExistingSchedule(ScheduleItem schedule)
    {
        EditingScheduleId = schedule.Id;
        IsEditingCompleted = schedule.IsCompleted;

        NewTitle = schedule.Title;
        NewDescription = schedule.Description;

        NewStartDate = schedule.StartAt.Date;
        NewStartTime = schedule.StartAt.ToString("HH:mm");

        NewEndDate = schedule.EndAt.Date;
        NewEndTime = schedule.EndAt.ToString("HH:mm");

        NewIsAllDay = schedule.IsAllDay;

        NewIsReminderEnabled = schedule.IsReminderEnabled;
        NewReminderMinutesBefore = schedule.IsReminderEnabled ? schedule.ReminderMinutesBefore : 10;

        ErrorMessage = string.Empty;

        IsEditorOpen = true;
    }

    /// <summary>
    /// 일정 편집 상태를 종료합니다.
    /// </summary>
    private void CloseEditor()
    {
        IsEditorOpen = false;
        EditingScheduleId = null;
        IsEditingCompleted = false;

        ResetInput();
    }

    /// <summary>
    /// 일정 입력값을 기본 상태로 되돌립니다.
    /// 현재 선택된 날짜를 새 일정의 기본 날짜로 사용합니다.
    /// </summary>
    private void ResetInput()
    {
        NewTitle = string.Empty;
        NewDescription = string.Empty;

        NewStartDate = SelectedDate;
        NewStartTime = "09:00";

        NewEndDate = SelectedDate;
        NewEndTime = "10:00";

        NewIsAllDay = false;

        NewIsReminderEnabled = false;
        NewReminderMinutesBefore = 10;

        ErrorMessage = string.Empty;
    }

    /// <summary>
    /// 한 주 안에서 여러 날짜 일정의 실제 표시 범위를 계산할 때 사용하는 내부 데이터입니다.
    /// 아직 화면용 ViewModel을 생성하기 전의 임시 계산 결과입니다.
    /// </summary>
    private readonly record struct MonthlySpanningScheduleCandidate(
        ScheduleItem Schedule,
        DateTime VisibleStartDate,
        DateTime VisibleEndDate,
        int StartDayIndex,
        int EndDayIndex,
        bool ContinuesToNextWeek);
}