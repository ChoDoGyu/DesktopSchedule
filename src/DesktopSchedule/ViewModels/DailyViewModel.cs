using System.Collections.ObjectModel;
using DesktopSchedule.Commands;
using DesktopSchedule.Models;
using DesktopSchedule.Services;
using DesktopSchedule.Utilities;

namespace DesktopSchedule.ViewModels;

/// <summary>
/// 선택한 날짜의 일간 일정 화면 상태를 관리합니다.
/// 같은 일정 데이터를 24시간 타임라인과 미완료/완료 일정 목록의 서로 다른 형태로 제공합니다.
/// </summary>
public class DailyViewModel : ScheduleEditorViewModelBase
{
    private DateTime _selectedDate;

    /// <summary>
    /// 선택 날짜의 24시간 타임라인에 표시할 시간 일정입니다.
    /// 기본적으로 미완료 일정만 표시하며 완료 일정 보기가 활성화되면 완료 일정도 함께 표시합니다.
    /// </summary>
    public ObservableCollection<DailyScheduleViewModel> TimedSchedules { get; } = new();

    /// <summary>
    /// 선택 날짜에 포함된 미완료 일정 목록입니다.
    /// </summary>
    public ObservableCollection<DailyScheduleViewModel> IncompleteSchedules { get; } = new();

    /// <summary>
    /// 선택 날짜에 포함된 완료 일정 목록입니다.
    /// </summary>
    public ObservableCollection<DailyScheduleViewModel> CompletedSchedules { get; } = new();

    /// <summary>
    /// 현재 일간 화면에서 기준으로 사용하는 날짜입니다.
    /// </summary>
    public DateTime SelectedDate
    {
        get => _selectedDate;
        private set
        {
            if (SetProperty(ref _selectedDate, value.Date))
            {
                OnPropertyChanged(nameof(SelectedDateText));
            }
        }
    }

    /// <summary>
    /// 화면 상단에 표시할 선택 날짜 문자열입니다.
    /// </summary>
    public string SelectedDateText => $"{SelectedDate:yyyy년 M월 d일} {GetDayName(SelectedDate.DayOfWeek)}요일";

    /// <summary>
    /// 하루 전으로 이동합니다.
    /// </summary>
    public RelayCommand PreviousDayCommand { get; }

    /// <summary>
    /// 실제 오늘 날짜로 이동합니다.
    /// </summary>
    public RelayCommand TodayCommand { get; }

    /// <summary>
    /// 하루 뒤로 이동합니다.
    /// </summary>
    public RelayCommand NextDayCommand { get; }

    public DailyViewModel(ScheduleService scheduleService) : base(scheduleService)
    {
        _selectedDate = DateTime.Today;

        PreviousDayCommand = new RelayCommand(_ => MoveDay(-1));
        TodayCommand = new RelayCommand(_ => MoveToToday());
        NextDayCommand = new RelayCommand(_ => MoveDay(1));

        LoadSchedules();
    }

    /// <summary>
    /// 일정 편집기의 기본 날짜로 현재 선택 날짜를 사용합니다.
    /// </summary>
    protected override DateTime GetSelectedDateForEditor()
    {
        return SelectedDate;
    }

    /// <summary>
    /// 일정 데이터 또는 완료 일정 표시 설정이 변경되면 화면 전체를 다시 구성합니다.
    /// </summary>
    protected override void RefreshSchedules()
    {
        LoadSchedules();
    }

    /// <summary>
    /// Drag한 시간 일정을 선택 날짜의 지정된 정각으로 이동합니다.
    /// 기존 일정의 전체 길이는 ScheduleService.Move에서 유지합니다.
    /// </summary>
    public void MoveScheduleByDrop(ScheduleItem schedule, int targetHour)
    {
        ArgumentNullException.ThrowIfNull(schedule);

        if (targetHour < 0 || targetHour > 23)
        {
            throw new ArgumentOutOfRangeException(nameof(targetHour));
        }

        if (schedule.IsAllDay)
        {
            return;
        }

        var newStartAt = SelectedDate.Date.AddHours(targetHour);

        if (schedule.StartAt == newStartAt)
        {
            ErrorMessage = string.Empty;
            return;
        }

        try
        {
            ScheduleService.Move(schedule.Id, newStartAt);
            ErrorMessage = string.Empty;
            LoadSchedules();
        }
        catch (KeyNotFoundException exception)
        {
            ErrorMessage = exception.Message;
        }
    }

    /// <summary>
    /// 할 일 목록에서 완료한 일 영역으로 Drag한 일정을 완료 상태로 변경합니다.
    /// </summary>
    public void MarkScheduleAsCompletedByDrop(ScheduleItem schedule)
    {
        ArgumentNullException.ThrowIfNull(schedule);

        if (schedule.IsCompleted)
        {
            return;
        }

        try
        {
            ScheduleService.MarkAsCompleted(schedule.Id);
            ErrorMessage = string.Empty;
            LoadSchedules();
        }
        catch (KeyNotFoundException exception)
        {
            ErrorMessage = exception.Message;
        }
    }

    /// <summary>
    /// 완료한 일 목록에서 할 일 영역으로 Drag한 일정의 완료 상태를 취소합니다.
    /// </summary>
    public void MarkScheduleAsIncompleteByDrop(ScheduleItem schedule)
    {
        ArgumentNullException.ThrowIfNull(schedule);

        if (!schedule.IsCompleted)
        {
            return;
        }

        try
        {
            ScheduleService.MarkAsIncomplete(schedule.Id);
            ErrorMessage = string.Empty;
            LoadSchedules();
        }
        catch (KeyNotFoundException exception)
        {
            ErrorMessage = exception.Message;
        }
    }

    /// <summary>
    /// 선택 날짜에 포함되는 전체 일정을 불러와 시간 타임라인, 미완료 목록, 완료 목록으로 각각 구성합니다.
    /// </summary>
    private void LoadSchedules()
    {
        TimedSchedules.Clear();
        IncompleteSchedules.Clear();
        CompletedSchedules.Clear();

        var schedules = ScheduleService
            .GetAll(true)
            .Where(schedule => ScheduleCalendarCalculator.IsScheduleOnDate(schedule, SelectedDate))
            .OrderBy(schedule => schedule.IsAllDay ? 0 : 1)
            .ThenBy(schedule => schedule.StartAt)
            .ThenBy(schedule => schedule.EndAt)
            .ThenBy(schedule => schedule.Title)
            .ToList();

        LoadTimedSchedules(schedules);
        LoadScheduleLists(schedules);
    }

    /// <summary>
    /// 선택 날짜의 시간 일정을 추리고 공통 계산기를 이용해
    /// 24시간 타임라인의 겹침 열 배치를 구성합니다.
    /// </summary>
    private void LoadTimedSchedules(IReadOnlyList<ScheduleItem> schedules)
    {
        var timedSchedules = schedules
            .Where(schedule => !schedule.IsAllDay && (ShowCompletedSchedules || !schedule.IsCompleted))
            .ToList();

        var placements = DailyScheduleLayoutCalculator.CreateLayout(timedSchedules, SelectedDate);

        foreach (var placement in placements)
        {
            TimedSchedules.Add(new DailyScheduleViewModel(placement.Schedule, SelectedDate, placement.ColumnIndex, placement.ColumnCount));
        }
    }

    /// <summary>
    /// 선택 날짜의 일정 데이터를 완료 상태에 따라 할 일과 완료한 일 목록으로 분리합니다.
    /// </summary>
    private void LoadScheduleLists(IReadOnlyList<ScheduleItem> schedules)
    {
        foreach (var schedule in schedules)
        {
            var scheduleViewModel = new DailyScheduleViewModel(schedule, SelectedDate, 0, 1);

            if (schedule.IsCompleted)
            {
                CompletedSchedules.Add(scheduleViewModel);
            }
            else
            {
                IncompleteSchedules.Add(scheduleViewModel);
            }
        }
    }

    /// <summary>
    /// 현재 선택 날짜를 지정한 일수만큼 이동합니다.
    /// </summary>
    private void MoveDay(int dayOffset)
    {
        SelectedDate = SelectedDate.AddDays(dayOffset);
        CloseEditor();
        LoadSchedules();
    }

    /// <summary>
    /// 현재 선택 날짜를 실제 오늘 날짜로 되돌립니다.
    /// </summary>
    private void MoveToToday()
    {
        SelectedDate = DateTime.Today;
        CloseEditor();
        LoadSchedules();
    }

    /// <summary>
    /// DayOfWeek 값을 한글 요일 이름으로 변환합니다.
    /// </summary>
    private static string GetDayName(DayOfWeek dayOfWeek)
    {
        return dayOfWeek switch
        {
            DayOfWeek.Sunday => "일",
            DayOfWeek.Monday => "월",
            DayOfWeek.Tuesday => "화",
            DayOfWeek.Wednesday => "수",
            DayOfWeek.Thursday => "목",
            DayOfWeek.Friday => "금",
            DayOfWeek.Saturday => "토",
            _ => string.Empty
        };
    }
}