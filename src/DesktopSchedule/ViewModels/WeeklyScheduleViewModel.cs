using System.Collections.ObjectModel;
using DesktopSchedule.Commands;
using DesktopSchedule.Models;
using DesktopSchedule.Services;

namespace DesktopSchedule.ViewModels;

/// <summary>
/// 주간 7일 일정 화면의 상태와 사용자 동작을 관리합니다.
/// 일정 조회, 생성, 수정, 삭제, 완료 처리와 Drag 이동 기능을 제공합니다.
/// </summary>
public class WeeklyScheduleViewModel : ViewModelBase
{
    // 여러 날짜 일정 한 행이 사용하는 화면 높이입니다.
    private const double SpanningScheduleRowHeight = 31.0;

    // 여러 날짜 일정 영역 아래쪽에 추가하는 여백입니다.
    private const double SpanningScheduleAreaPadding = 4.0;

    // 일정 데이터의 조회와 변경을 담당하는 공통 Service입니다.
    private readonly ScheduleService _scheduleService;

    // 현재 화면에 표시하고 있는 주의 시작 날짜입니다.
    private DateTime _weekStartDate;

    // 사용자가 현재 선택한 날짜입니다.
    private DateTime _selectedDate;

    // 현재 수정 중인 기존 일정의 Id입니다.
    // null이면 새 일정 작성 상태입니다.
    private Guid? _editingScheduleId;

    // 일정 편집기를 화면에 표시할지 여부입니다.
    private bool _isEditorOpen;

    // 현재 편집 중인 일정이 완료 상태인지 여부입니다.
    private bool _isEditingCompleted;

    // 완료된 일정까지 주간 화면에 표시할지 여부입니다.
    private bool _showCompletedSchedules;

    // 일정 제목 입력값입니다.
    private string _newTitle = string.Empty;

    // 일정 설명 입력값입니다.
    private string _newDescription = string.Empty;

    // 일정 시작 날짜 입력값입니다.
    private DateTime? _newStartDate = DateTime.Today;

    // 일정 시작 시간 입력값입니다.
    private string _newStartTime = "09:00";

    // 일정 종료 날짜 입력값입니다.
    private DateTime? _newEndDate = DateTime.Today;

    // 일정 종료 시간 입력값입니다.
    private string _newEndTime = "10:00";

    // 하루 종일 일정인지 여부입니다.
    private bool _newIsAllDay;

    // 알림 기능을 사용할지 여부입니다.
    private bool _newIsReminderEnabled;

    // 일정 시작 몇 분 전에 알림을 표시할지 저장합니다.
    private int _newReminderMinutesBefore = 10;

    // 일정 입력이나 변경 과정에서 발생한 오류 메시지입니다.
    private string _errorMessage = string.Empty;

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
    /// 일정 알림에서 선택할 수 있는 시작 전 시간 목록입니다.
    /// </summary>
    public IReadOnlyList<int> ReminderMinuteOptions { get; } = new[] { 0, 5, 10, 30, 60, 1440 };

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
    /// 현재 편집 중인 기존 일정의 Id입니다.
    /// null이면 새 일정 작성 상태입니다.
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
    /// 기존 일정을 수정하고 있는지 여부입니다.
    /// </summary>
    public bool IsEditMode => EditingScheduleId.HasValue;

    /// <summary>
    /// 일정 편집 화면의 제목입니다.
    /// </summary>
    public string EditorTitle => IsEditMode ? "일정 수정" : "새 일정";

    /// <summary>
    /// 현재 편집 중인 일정의 완료 상태입니다.
    /// 완료/완료 취소 버튼 표시를 구분할 때 사용합니다.
    /// </summary>
    public bool IsEditingCompleted
    {
        get => _isEditingCompleted;
        private set => SetProperty(ref _isEditingCompleted, value);
    }

    /// <summary>
    /// 일정 편집기를 표시할지 여부입니다.
    /// </summary>
    public bool IsEditorOpen
    {
        get => _isEditorOpen;
        set => SetProperty(ref _isEditorOpen, value);
    }

    /// <summary>
    /// 완료된 일정도 주간 화면에 표시할지 여부입니다.
    /// 값이 변경되면 현재 주의 일정 데이터를 다시 불러옵니다.
    /// </summary>
    public bool ShowCompletedSchedules
    {
        get => _showCompletedSchedules;
        set
        {
            if (SetProperty(ref _showCompletedSchedules, value))
            {
                LoadWeek();
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
    /// 일정 시작 날짜 입력값입니다.
    /// </summary>
    public DateTime? NewStartDate
    {
        get => _newStartDate;
        set => SetProperty(ref _newStartDate, value);
    }

    /// <summary>
    /// 일정 시작 시간 입력값입니다.
    /// </summary>
    public string NewStartTime
    {
        get => _newStartTime;
        set => SetProperty(ref _newStartTime, value);
    }

    /// <summary>
    /// 일정 종료 날짜 입력값입니다.
    /// </summary>
    public DateTime? NewEndDate
    {
        get => _newEndDate;
        set => SetProperty(ref _newEndDate, value);
    }

    /// <summary>
    /// 일정 종료 시간 입력값입니다.
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
    /// 일정 알림을 사용할지 여부입니다.
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
    /// 사용자에게 표시할 오류 메시지입니다.
    /// </summary>
    public string ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
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
    /// 사용자가 클릭한 날짜를 선택한 뒤
    /// 그 날짜를 기본값으로 사용하는 새 일정 편집기를 엽니다.
    /// </summary>
    public RelayCommand OpenNewScheduleForDateCommand { get; }

    /// <summary>
    /// 현재 선택 날짜를 기준으로 새 일정 편집기를 엽니다.
    /// </summary>
    public RelayCommand OpenNewScheduleCommand { get; }

    /// <summary>
    /// 기존 일정을 수정하기 위해 선택합니다.
    /// </summary>
    public RelayCommand SelectScheduleCommand { get; }

    /// <summary>
    /// 새 일정을 저장하거나 기존 일정을 수정합니다.
    /// </summary>
    public RelayCommand SaveScheduleCommand { get; }

    /// <summary>
    /// 현재 편집 중인 일정을 삭제합니다.
    /// </summary>
    public RelayCommand DeleteScheduleCommand { get; }

    /// <summary>
    /// 현재 편집 중인 일정을 완료 상태로 변경합니다.
    /// </summary>
    public RelayCommand MarkAsCompletedCommand { get; }

    /// <summary>
    /// 현재 편집 중인 일정의 완료 상태를 취소합니다.
    /// </summary>
    public RelayCommand MarkAsIncompleteCommand { get; }

    /// <summary>
    /// 현재 일정 편집을 취소합니다.
    /// </summary>
    public RelayCommand CancelEditCommand { get; }

    public WeeklyScheduleViewModel(ScheduleService scheduleService)
    {
        _scheduleService = scheduleService ?? throw new ArgumentNullException(nameof(scheduleService));

        PreviousWeekCommand = new RelayCommand(_ => MoveWeek(-1));
        CurrentWeekCommand = new RelayCommand(_ => MoveToCurrentWeek());
        NextWeekCommand = new RelayCommand(_ => MoveWeek(1));

        SelectDateCommand = new RelayCommand(parameter => SelectDate(parameter as WeeklyDayViewModel));
        OpenNewScheduleForDateCommand = new RelayCommand(parameter => OpenNewScheduleForDate(parameter as WeeklyDayViewModel));

        OpenNewScheduleCommand = new RelayCommand(_ => OpenNewSchedule());
        SelectScheduleCommand = new RelayCommand(parameter => SelectSchedule(parameter as ScheduleItem));
        SaveScheduleCommand = new RelayCommand(_ => SaveSchedule());
        DeleteScheduleCommand = new RelayCommand(_ => DeleteSchedule(), _ => IsEditMode);
        MarkAsCompletedCommand = new RelayCommand(_ => MarkAsCompleted(), _ => IsEditMode && !IsEditingCompleted);
        MarkAsIncompleteCommand = new RelayCommand(_ => MarkAsIncomplete(), _ => IsEditMode && IsEditingCompleted);
        CancelEditCommand = new RelayCommand(_ => CloseEditor());

        WeekStartDate = GetWeekStart(DateTime.Today);
        SelectedDate = DateTime.Today;

        LoadWeek();
    }

    /// <summary>
    /// 현재 주의 날짜별 일정과 여러 날짜 연결 일정을 다시 구성합니다.
    /// 완료 일정 표시 여부도 이 시점에 적용됩니다.
    /// </summary>
    private void LoadWeek()
    {
        Days.Clear();
        SpanningSchedules.Clear();

        // 월간 화면과 동일하게 ShowCompletedSchedules 값을 Service에 전달합니다.
        // 기본값 false에서는 완료 일정이 제외되고,
        // 사용자가 체크하면 완료 일정까지 함께 조회합니다.
        var schedules = _scheduleService.GetAll(ShowCompletedSchedules);

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
    /// 주간 화면에서 날짜를 직접 클릭했을 때 호출됩니다.
    /// 날짜를 선택한 뒤 그 날짜를 시작/종료 기본값으로 새 일정 편집기를 엽니다.
    /// </summary>
    private void OpenNewScheduleForDate(WeeklyDayViewModel? selectedDay)
    {
        if (selectedDay is null)
        {
            return;
        }

        SelectDate(selectedDay);
        OpenNewSchedule();
    }

    /// <summary>
    /// 현재 선택된 날짜를 기준으로 새 일정 편집기를 준비합니다.
    /// </summary>
    private void OpenNewSchedule()
    {
        EditingScheduleId = null;
        IsEditingCompleted = false;

        ResetInput();

        NewStartDate = SelectedDate;
        NewEndDate = SelectedDate;

        IsEditorOpen = true;
    }

    /// <summary>
    /// 기존 일정의 값을 편집 입력 상태에 복사합니다.
    /// </summary>
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

    /// <summary>
    /// 현재 입력된 값으로 새 일정을 생성하거나 기존 일정을 수정합니다.
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
    /// 계산이 완료된 시작/종료 시간을 실제 ScheduleService에 전달합니다.
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

    /// <summary>
    /// 현재 편집 중인 일정을 삭제합니다.
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
            LoadWeek();
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
            LoadWeek();
            CloseEditor();
        }
        catch (KeyNotFoundException exception)
        {
            ErrorMessage = exception.Message;
        }
    }

    /// <summary>
    /// 현재 편집 중인 일정의 완료 상태를 취소합니다.
    /// 월간 화면과 동일하게 완료 일정을 다시 일반 일정으로 되돌립니다.
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
            LoadWeek();
            CloseEditor();
        }
        catch (KeyNotFoundException exception)
        {
            ErrorMessage = exception.Message;
        }
    }

    /// <summary>
    /// 일정 편집기를 닫고 편집 상태를 초기화합니다.
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
    /// 현재 선택된 날짜를 시작/종료 기본 날짜로 사용합니다.
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
    /// 현재 표시 중인 주를 지정한 주 수만큼 이동합니다.
    /// </summary>
    private void MoveWeek(int weekOffset)
    {
        WeekStartDate = WeekStartDate.AddDays(7 * weekOffset);
        SelectedDate = WeekStartDate;
        IsEditorOpen = false;

        LoadWeek();
    }

    /// <summary>
    /// 오늘이 포함된 현재 주로 이동합니다.
    /// </summary>
    private void MoveToCurrentWeek()
    {
        WeekStartDate = GetWeekStart(DateTime.Today);
        SelectedDate = DateTime.Today;
        IsEditorOpen = false;

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