using System.Collections.ObjectModel;
using DesktopSchedule.Commands;
using DesktopSchedule.Models;
using DesktopSchedule.Services;
using DesktopSchedule.ViewModels.Layout;

namespace DesktopSchedule.ViewModels;

/// <summary>
/// 주간 일정 화면의 상태와 사용자 동작을 관리합니다.
/// 시간축과 일정 배치 계산은 전용 Calculator에 위임합니다.
/// </summary>
public class WeeklyScheduleViewModel : ViewModelBase
{
    private const int DragSnapMinutes = 15;

    private readonly ScheduleService _scheduleService;
    private readonly WeeklyTimelineLayoutCalculator _timelineLayoutCalculator;
    private readonly WeeklyScheduleLayoutCalculator _scheduleLayoutCalculator;

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

    public ObservableCollection<WeeklyDayViewModel> Days { get; } = new();

    public ObservableCollection<WeeklyTimelineSegmentViewModel> TimelineSegments { get; } = new();

    public double HourHeight => _timelineLayoutCalculator.HourHeight;

    public double TimelineDisplayHeight => _timelineLayoutCalculator.GetDisplayHeight(TimelineSegments);

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

        _timelineLayoutCalculator = new WeeklyTimelineLayoutCalculator();
        _scheduleLayoutCalculator = new WeeklyScheduleLayoutCalculator(_timelineLayoutCalculator);

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
    /// Drag & Drop된 시간 일정을 지정한 요일과 시간으로 이동합니다.
    /// </summary>
    public void MoveScheduleByDrop(ScheduleItem schedule, int targetDayIndex, double timelineOffset)
    {
        if (schedule is null || schedule.IsAllDay)
        {
            return;
        }

        if (targetDayIndex < 0 || targetDayIndex > 6)
        {
            return;
        }

        var timelineSegments = TimelineSegments.ToList();
        var targetMinutes = _timelineLayoutCalculator.GetMinutesFromTimelineOffset(timelineSegments, timelineOffset);

        targetMinutes = SnapMinutes(targetMinutes);
        targetMinutes = Math.Clamp(targetMinutes, 0, 1425);

        var newStartAt = WeekStartDate.AddDays(targetDayIndex).AddMinutes(targetMinutes);

        try
        {
            _scheduleService.Move(schedule.Id, newStartAt);

            SelectedDate = newStartAt.Date;
            ErrorMessage = string.Empty;

            LoadWeek();
        }
        catch (KeyNotFoundException exception)
        {
            ErrorMessage = exception.Message;
        }
    }

    private void LoadWeek()
    {
        var schedules = _scheduleService.GetAll();

        LoadTimeline(schedules);
        LoadDays(schedules);
    }

    private void LoadTimeline(IReadOnlyList<ScheduleItem> schedules)
    {
        TimelineSegments.Clear();

        var segments = _timelineLayoutCalculator.CreateSegments(schedules, WeekStartDate);

        foreach (var segment in segments)
        {
            TimelineSegments.Add(segment);
        }

        OnPropertyChanged(nameof(TimelineDisplayHeight));
    }

    private void LoadDays(IReadOnlyList<ScheduleItem> schedules)
    {
        Days.Clear();

        var timelineSegments = TimelineSegments.ToList();

        for (var dayOffset = 0; dayOffset < 7; dayOffset++)
        {
            var date = WeekStartDate.AddDays(dayOffset);
            var isSelected = date.Date == SelectedDate.Date;

            Days.Add(_scheduleLayoutCalculator.CreateDay(date, isSelected, schedules, timelineSegments));
        }
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

    /// <summary>
    /// Drag & Drop 시간을 15분 단위로 맞춥니다.
    /// </summary>
    private static int SnapMinutes(int minutes)
    {
        return (int)Math.Round(minutes / (double)DragSnapMinutes, MidpointRounding.AwayFromZero) * DragSnapMinutes;
    }

    private static DateTime GetWeekStart(DateTime date)
    {
        return date.Date.AddDays(-(int)date.DayOfWeek);
    }
}