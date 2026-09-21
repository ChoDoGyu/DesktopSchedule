using System.Collections.ObjectModel;
using DesktopSchedule.Commands;
using DesktopSchedule.Models;
using DesktopSchedule.Services;

namespace DesktopSchedule.ViewModels;

/// <summary>
/// 주간 7일 일정 화면의 상태와 사용자 동작을 관리합니다.
/// </summary>
public class WeeklyScheduleViewModel : ViewModelBase
{
    private const double SpanningScheduleRowHeight = 31.0;
    private const double SpanningScheduleAreaPadding = 4.0;

    private readonly ScheduleService _scheduleService;

    private DateTime _weekStartDate;
    private DateTime _selectedDate;
    private Guid? _editingScheduleId;
    private bool _isEditorOpen;
    private bool _isEditingCompleted;
    private string _newTitle = string.Empty;
    private string _newDescription = string.Empty;
    private DateTime? _newStartDate = DateTime.Today;
    private string _newStartTime = "09:00";
    private DateTime? _newEndDate = DateTime.Today;
    private string _newEndTime = "10:00";
    private bool _newIsAllDay;
    private bool _newIsReminderEnabled;
    private int _newReminderMinutesBefore = 10;
    private string _errorMessage = string.Empty;
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
    /// 일정 알림에서 선택할 수 있는 시작 전 시간 목록입니다.
    /// </summary>
    public IReadOnlyList<int> ReminderMinuteOptions { get; } = new[] { 0, 5, 10, 30, 60, 1440 };

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

    public DateTime WeekEndDate => WeekStartDate.AddDays(6);

    public string WeekRangeText => $"{WeekStartDate:yyyy.MM.dd} ~ {WeekEndDate:yyyy.MM.dd}";

    public DateTime SelectedDate
    {
        get => _selectedDate;
        private set => SetProperty(ref _selectedDate, value);
    }

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

    public bool IsEditMode => EditingScheduleId.HasValue;

    public string EditorTitle => IsEditMode ? "일정 수정" : "새 일정";

    public bool IsEditingCompleted
    {
        get => _isEditingCompleted;
        private set => SetProperty(ref _isEditingCompleted, value);
    }

    public bool IsEditorOpen
    {
        get => _isEditorOpen;
        set => SetProperty(ref _isEditorOpen, value);
    }

    public string NewTitle
    {
        get => _newTitle;
        set => SetProperty(ref _newTitle, value);
    }

    public string NewDescription
    {
        get => _newDescription;
        set => SetProperty(ref _newDescription, value);
    }

    public DateTime? NewStartDate
    {
        get => _newStartDate;
        set => SetProperty(ref _newStartDate, value);
    }

    public string NewStartTime
    {
        get => _newStartTime;
        set => SetProperty(ref _newStartTime, value);
    }

    public DateTime? NewEndDate
    {
        get => _newEndDate;
        set => SetProperty(ref _newEndDate, value);
    }

    public string NewEndTime
    {
        get => _newEndTime;
        set => SetProperty(ref _newEndTime, value);
    }

    public bool NewIsAllDay
    {
        get => _newIsAllDay;
        set => SetProperty(ref _newIsAllDay, value);
    }

    public bool NewIsReminderEnabled
    {
        get => _newIsReminderEnabled;
        set => SetProperty(ref _newIsReminderEnabled, value);
    }

    public int NewReminderMinutesBefore
    {
        get => _newReminderMinutesBefore;
        set => SetProperty(ref _newReminderMinutesBefore, value);
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    public RelayCommand PreviousWeekCommand { get; }
    public RelayCommand CurrentWeekCommand { get; }
    public RelayCommand NextWeekCommand { get; }
    public RelayCommand SelectDateCommand { get; }
    public RelayCommand OpenNewScheduleCommand { get; }
    public RelayCommand SelectScheduleCommand { get; }
    public RelayCommand SaveScheduleCommand { get; }
    public RelayCommand DeleteScheduleCommand { get; }
    public RelayCommand MarkAsCompletedCommand { get; }
    public RelayCommand CancelEditCommand { get; }

    public WeeklyScheduleViewModel(ScheduleService scheduleService)
    {
        _scheduleService = scheduleService ?? throw new ArgumentNullException(nameof(scheduleService));

        PreviousWeekCommand = new RelayCommand(_ => MoveWeek(-1));
        CurrentWeekCommand = new RelayCommand(_ => MoveToCurrentWeek());
        NextWeekCommand = new RelayCommand(_ => MoveWeek(1));
        SelectDateCommand = new RelayCommand(parameter => SelectDate(parameter as WeeklyDayViewModel));
        OpenNewScheduleCommand = new RelayCommand(_ => OpenNewSchedule());
        SelectScheduleCommand = new RelayCommand(parameter => SelectSchedule(parameter as ScheduleItem));
        SaveScheduleCommand = new RelayCommand(_ => SaveSchedule());
        DeleteScheduleCommand = new RelayCommand(_ => DeleteSchedule(), _ => IsEditMode);
        MarkAsCompletedCommand = new RelayCommand(_ => MarkAsCompleted(), _ => IsEditMode && !IsEditingCompleted);
        CancelEditCommand = new RelayCommand(_ => CloseEditor());

        WeekStartDate = GetWeekStart(DateTime.Today);
        SelectedDate = DateTime.Today;

        LoadWeek();
    }

    /// <summary>
    /// 현재 주의 날짜별 일정과 여러 날짜 연결 일정을 다시 구성합니다.
    /// </summary>
    private void LoadWeek()
    {
        Days.Clear();
        SpanningSchedules.Clear();

        var schedules = _scheduleService.GetAll();

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
            var rowIndex = FindAvailableSpanningRow(
                rowEndDayIndices,
                candidate.StartDayIndex);

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
            _scheduleService.Move(schedule.Id, newStartAt);

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

    private void OpenNewSchedule()
    {
        EditingScheduleId = null;
        IsEditingCompleted = false;

        ResetInput();

        NewStartDate = SelectedDate;
        NewEndDate = SelectedDate;

        IsEditorOpen = true;
    }

    private void SelectSchedule(ScheduleItem? schedule)
    {
        if (schedule is null)
        {
            return;
        }

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

            LoadWeek();
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

    private void DeleteSchedule()
    {
        if (!EditingScheduleId.HasValue)
        {
            return;
        }

        try
        {
            _scheduleService.Delete(EditingScheduleId.Value);
            LoadWeek();
            CloseEditor();
        }
        catch (KeyNotFoundException exception)
        {
            ErrorMessage = exception.Message;
        }
    }

    private void MarkAsCompleted()
    {
        if (!EditingScheduleId.HasValue)
        {
            return;
        }

        try
        {
            _scheduleService.MarkAsCompleted(EditingScheduleId.Value);
            LoadWeek();
            CloseEditor();
        }
        catch (KeyNotFoundException exception)
        {
            ErrorMessage = exception.Message;
        }
    }

    private void CloseEditor()
    {
        IsEditorOpen = false;
        EditingScheduleId = null;
        IsEditingCompleted = false;

        ResetInput();
    }

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

    private void MoveWeek(int weekOffset)
    {
        WeekStartDate = WeekStartDate.AddDays(7 * weekOffset);
        SelectedDate = WeekStartDate;
        IsEditorOpen = false;

        LoadWeek();
    }

    private void MoveToCurrentWeek()
    {
        WeekStartDate = GetWeekStart(DateTime.Today);
        SelectedDate = DateTime.Today;
        IsEditorOpen = false;

        LoadWeek();
    }

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