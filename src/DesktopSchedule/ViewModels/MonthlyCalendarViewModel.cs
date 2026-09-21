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
    // 일정 조회, 생성, 수정, 삭제 기능을 제공하는 Service입니다.
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
    /// 현재 화면에서 표시할 일정 목록입니다.
    /// 기존 임시 목록 UI와 일정 CRUD 기능을 유지하기 위해 사용합니다.
    /// 이후 월간 달력 일정 배치 기능에서도 같은 Schedule 데이터를 사용합니다.
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

        // DayOfWeek에서 Sunday는 0이므로,
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
    /// 저장된 일정을 다시 불러와 화면에서 사용할 목록을 갱신합니다.
    /// </summary>
    public void LoadSchedules()
    {
        Schedules.Clear();

        var schedules = _scheduleService.GetAll(ShowCompletedSchedules);

        foreach (var schedule in schedules)
        {
            Schedules.Add(schedule);
        }
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
}