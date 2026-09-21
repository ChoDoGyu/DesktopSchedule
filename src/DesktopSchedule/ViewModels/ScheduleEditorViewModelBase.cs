using System.Windows.Input; // CommandManager를 사용해 Command 실행 가능 상태를 즉시 다시 검사하기 위해 필요합니다.
using DesktopSchedule.Commands; // RelayCommand를 사용하기 위해 필요합니다.
using DesktopSchedule.Models; // ScheduleItem을 사용하기 위해 필요합니다.
using DesktopSchedule.Services; // ScheduleService를 사용하기 위해 필요합니다.

namespace DesktopSchedule.ViewModels;

/// <summary>
/// 주간과 월간 일정 화면에서 공통으로 사용하는
/// 일정 편집 상태와 CRUD 동작을 관리하는 기반 ViewModel입니다.
/// </summary>
/// <remarks>
/// 날짜 배치 방식이나 달력 구성은 주간과 월간이 서로 다르므로
/// 이 클래스에서는 일정 편집과 저장에 관한 공통 책임만 관리합니다.
/// </remarks>
public abstract class ScheduleEditorViewModelBase : ViewModelBase
{
    // 실제 일정 데이터의 조회와 변경을 담당하는 Service입니다.
    // 주간과 월간 ViewModel에서도 Drag 이동과 일정 조회에 사용할 수 있도록
    // protected 속성으로 제공합니다.
    protected ScheduleService ScheduleService { get; }

    // 현재 편집 중인 기존 일정의 Id입니다.
    // null이면 새 일정을 작성하는 상태입니다.
    private Guid? _editingScheduleId;

    // 현재 편집 중인 일정의 완료 상태입니다.
    private bool _isEditingCompleted;

    // 일정 편집기를 화면에 표시할지 여부입니다.
    private bool _isEditorOpen;

    // 완료된 일정도 달력에 표시할지 여부입니다.
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

    // 일정 알림을 사용할지 여부입니다.
    private bool _newIsReminderEnabled;

    // 일정 시작 몇 분 전에 알림을 발생시킬지 저장합니다.
    private int _newReminderMinutesBefore = 10;

    // 일정 입력이나 변경 과정에서 사용자에게 표시할 오류 메시지입니다.
    private string _errorMessage = string.Empty;

    /// <summary>
    /// 현재 편집 중인 기존 일정의 Id입니다.
    /// null이면 새 일정을 작성하고 있는 상태입니다.
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

                // DeleteScheduleCommand와 완료 관련 Command는
                // IsEditMode를 CanExecute 조건으로 사용합니다.
                // 일정 선택 직후 버튼 상태가 즉시 갱신되도록
                // WPF에 Command 실행 가능 여부를 다시 검사하도록 요청합니다.
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }

    /// <summary>
    /// 기존 일정을 수정하고 있는지 여부입니다.
    /// </summary>
    public bool IsEditMode => EditingScheduleId.HasValue;

    /// <summary>
    /// 현재 편집 중인 일정이 완료 상태인지 여부입니다.
    /// 완료 또는 완료 취소 버튼 표시를 구분할 때 사용합니다.
    /// </summary>
    public bool IsEditingCompleted
    {
        get => _isEditingCompleted;
        private set
        {
            if (SetProperty(ref _isEditingCompleted, value))
            {
                // 완료와 완료 취소 Command는 현재 완료 상태를
                // CanExecute 조건으로 사용하므로 즉시 다시 평가합니다.
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }

    /// <summary>
    /// 일정 편집 화면에 표시할 제목입니다.
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
    /// 완료된 일정까지 달력에 표시할지 여부입니다.
    /// 값이 변경되면 파생 ViewModel이 자신의 일정 화면을 다시 구성합니다.
    /// </summary>
    public bool ShowCompletedSchedules
    {
        get => _showCompletedSchedules;
        set
        {
            if (SetProperty(ref _showCompletedSchedules, value))
            {
                RefreshSchedules();
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
    /// 일정 알림에서 선택할 수 있는 시작 전 시간 목록입니다.
    /// </summary>
    public IReadOnlyList<int> ReminderMinuteOptions { get; } = new[] { 0, 5, 10, 30, 60, 1440 };

    /// <summary>
    /// 현재 선택 날짜를 기준으로 새 일정 편집기를 엽니다.
    /// </summary>
    public RelayCommand OpenNewScheduleCommand { get; }

    /// <summary>
    /// 선택한 기존 일정을 편집 상태로 엽니다.
    /// </summary>
    public RelayCommand SelectScheduleCommand { get; }

    /// <summary>
    /// 입력된 값으로 새 일정을 추가하거나 기존 일정을 수정합니다.
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

    protected ScheduleEditorViewModelBase(ScheduleService scheduleService)
    {
        ScheduleService = scheduleService ?? throw new ArgumentNullException(nameof(scheduleService));

        OpenNewScheduleCommand = new RelayCommand(_ => PrepareNewSchedule());
        SelectScheduleCommand = new RelayCommand(parameter => SelectSchedule(parameter as ScheduleItem));
        SaveScheduleCommand = new RelayCommand(_ => SaveSchedule());
        DeleteScheduleCommand = new RelayCommand(_ => DeleteSchedule(), _ => IsEditMode);
        MarkAsCompletedCommand = new RelayCommand(_ => MarkAsCompleted(), _ => IsEditMode && !IsEditingCompleted);
        MarkAsIncompleteCommand = new RelayCommand(_ => MarkAsIncomplete(), _ => IsEditMode && IsEditingCompleted);
        CancelEditCommand = new RelayCommand(_ => CloseEditor());
    }

    /// <summary>
    /// 파생 ViewModel이 현재 선택하고 있는 날짜를 반환합니다.
    /// 새 일정의 시작/종료 기본 날짜를 결정할 때 사용합니다.
    /// </summary>
    protected abstract DateTime GetSelectedDateForEditor();

    /// <summary>
    /// 일정 데이터가 변경되었을 때
    /// 파생 ViewModel이 자신의 달력 화면을 다시 구성하도록 요청합니다.
    /// </summary>
    protected abstract void RefreshSchedules();

    /// <summary>
    /// 현재 선택 날짜를 기준으로 새 일정 입력 상태를 준비합니다.
    /// 주간 화면에서 날짜를 직접 클릭하는 경우에도 재사용할 수 있도록
    /// protected 메서드로 제공합니다.
    /// </summary>
    protected void PrepareNewSchedule()
    {
        EditingScheduleId = null;
        IsEditingCompleted = false;

        ResetInput();

        IsEditorOpen = true;
    }

    /// <summary>
    /// 일정 편집기를 닫고 편집 중인 상태와 입력값을 초기화합니다.
    /// 주간 또는 월간 이동 시에도 사용할 수 있도록 protected로 제공합니다.
    /// </summary>
    protected void CloseEditor()
    {
        IsEditorOpen = false;
        EditingScheduleId = null;
        IsEditingCompleted = false;

        ResetInput();
    }

    /// <summary>
    /// 선택한 기존 일정을 편집 상태로 엽니다.
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
    /// 현재 입력값을 검사하고 실제 저장에 사용할 시작/종료 시간을 계산합니다.
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
    /// 계산이 완료된 시작/종료 값을 ScheduleService에 전달해
    /// 새 일정을 추가하거나 기존 일정을 수정합니다.
    /// </summary>
    private void SaveSchedule(DateTime startAt, DateTime endAt)
    {
        try
        {
            if (EditingScheduleId.HasValue)
            {
                ScheduleService.Update(EditingScheduleId.Value, NewTitle, NewDescription, startAt, endAt, NewIsAllDay, NewIsReminderEnabled, NewReminderMinutesBefore);
            }
            else
            {
                ScheduleService.Add(NewTitle, NewDescription, startAt, endAt, NewIsAllDay, NewIsReminderEnabled, NewReminderMinutesBefore);
            }

            RefreshSchedules();
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
            ScheduleService.Delete(EditingScheduleId.Value);

            RefreshSchedules();
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
            ScheduleService.MarkAsCompleted(EditingScheduleId.Value);

            RefreshSchedules();
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
            ScheduleService.MarkAsIncomplete(EditingScheduleId.Value);

            RefreshSchedules();
            CloseEditor();
        }
        catch (KeyNotFoundException exception)
        {
            ErrorMessage = exception.Message;
        }
    }

    /// <summary>
    /// 일정 입력값을 기본 상태로 되돌립니다.
    /// 시작/종료 날짜는 각 화면에서 현재 선택한 날짜를 사용합니다.
    /// </summary>
    private void ResetInput()
    {
        var selectedDate = GetSelectedDateForEditor().Date;

        NewTitle = string.Empty;
        NewDescription = string.Empty;

        NewStartDate = selectedDate;
        NewStartTime = "09:00";

        NewEndDate = selectedDate;
        NewEndTime = "10:00";

        NewIsAllDay = false;

        NewIsReminderEnabled = false;
        NewReminderMinutesBefore = 10;

        ErrorMessage = string.Empty;
    }
}