using System.Collections.ObjectModel;
using DesktopSchedule.Commands;
using DesktopSchedule.Models;
using DesktopSchedule.Services;
using DesktopSchedule.Utilities;

namespace DesktopSchedule.ViewModels;

/// <summary>
/// 선택한 날짜의 일간 일정 화면 상태를 관리합니다.
/// 같은 일정 데이터를 24시간 타임라인과
/// 미완료/완료 일정 목록의 서로 다른 형태로 제공합니다.
/// </summary>
public class DailyViewModel : ScheduleEditorViewModelBase
{
    private DateTime _selectedDate;

    /// <summary>
    /// 선택 날짜의 24시간 타임라인에 표시할 시간 일정입니다.
    /// 기본적으로 미완료 일정만 표시하며,
    /// 완료 일정 보기가 활성화되면 완료 일정도 함께 표시합니다.
    /// </summary>
    public ObservableCollection<DailyScheduleViewModel> TimedSchedules { get; } = new();

    /// <summary>
    /// 선택 날짜에 포함된 미완료 일정 목록입니다.
    /// 일간 화면 오른쪽의 '할 일' 영역에 표시합니다.
    /// </summary>
    public ObservableCollection<DailyScheduleViewModel> IncompleteSchedules { get; } = new();

    /// <summary>
    /// 선택 날짜에 포함된 완료 일정 목록입니다.
    /// 일간 화면 오른쪽의 '완료한 일' 영역에 표시합니다.
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
    public string SelectedDateText =>
        $"{SelectedDate:yyyy년 M월 d일} {GetDayName(SelectedDate.DayOfWeek)}요일";

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
    /// 일정 데이터 또는 완료 일정 표시 설정이 변경되면
    /// 타임라인과 미완료/완료 목록을 모두 다시 구성합니다.
    /// </summary>
    protected override void RefreshSchedules()
    {
        LoadSchedules();
    }

    /// <summary>
    /// Drag한 시간 일정을 선택 날짜의 지정된 정각으로 이동합니다.
    /// 기존 일정의 전체 길이는 ScheduleService.Move에서 그대로 유지됩니다.
    /// </summary>
    public void MoveScheduleByDrop(ScheduleItem schedule, int targetHour)
    {
        ArgumentNullException.ThrowIfNull(schedule);

        if (targetHour < 0 || targetHour > 23)
        {
            throw new ArgumentOutOfRangeException(nameof(targetHour));
        }

        // 일간 시간표에는 시간 일정만 Drag할 수 있습니다.
        if (schedule.IsAllDay)
        {
            return;
        }

        var newStartAt =
            SelectedDate.Date.AddHours(targetHour);

        // 정확히 같은 시작 시각으로 Drop한 경우에는
        // 저장 작업 없이 Drag만 종료합니다.
        //
        // 13:15 일정을 13시 칸에 놓는 경우에는
        // newStartAt이 13:00이므로 동일하지 않으며
        // 실제로 13:00으로 Snap됩니다.
        if (schedule.StartAt == newStartAt)
        {
            ErrorMessage = string.Empty;
            return;
        }

        try
        {
            ScheduleService.Move(
                schedule.Id,
                newStartAt);

            ErrorMessage = string.Empty;

            // 왼쪽 타임라인뿐 아니라 오른쪽 할 일/완료 목록의
            // 표시 시간도 함께 갱신합니다.
            LoadSchedules();
        }
        catch (KeyNotFoundException exception)
        {
            ErrorMessage = exception.Message;
        }
    }

    /// <summary>
    /// 선택 날짜에 포함되는 전체 일정을 불러와
    /// 시간 타임라인, 미완료 목록, 완료 목록으로 각각 구성합니다.
    /// </summary>
    private void LoadSchedules()
    {
        TimedSchedules.Clear();
        IncompleteSchedules.Clear();
        CompletedSchedules.Clear();

        // 오른쪽 완료 목록을 항상 구성해야 하므로
        // 완료 여부와 관계없이 전체 일정을 가져옵니다.
        var schedules = ScheduleService
            .GetAll(true)
            .Where(schedule =>
                ScheduleCalendarCalculator.IsScheduleOnDate(
                    schedule,
                    SelectedDate))
            .OrderBy(schedule => schedule.IsAllDay ? 0 : 1)
            .ThenBy(schedule => schedule.StartAt)
            .ThenBy(schedule => schedule.EndAt)
            .ThenBy(schedule => schedule.Title)
            .ToList();

        LoadTimedSchedules(schedules);
        LoadScheduleLists(schedules);
    }

    /// <summary>
    /// 선택 날짜의 시간 일정을 추려
    /// 24시간 타임라인의 겹침 열과 실제 위치를 계산합니다.
    /// 완료 일정은 ShowCompletedSchedules 설정에 따라 포함합니다.
    /// </summary>
    private void LoadTimedSchedules(IReadOnlyList<ScheduleItem> schedules)
    {
        var timedSchedules = schedules
            .Where(schedule =>
                !schedule.IsAllDay &&
                (ShowCompletedSchedules || !schedule.IsCompleted))
            .OrderBy(schedule => GetVisibleStartAt(schedule))
            .ThenBy(schedule => GetVisibleEndAt(schedule))
            .ThenBy(schedule => schedule.Title)
            .ToList();

        var layouts = CreateScheduleLayouts(timedSchedules);

        foreach (var layout in layouts)
        {
            TimedSchedules.Add(
                new DailyScheduleViewModel(
                    layout.Schedule,
                    SelectedDate,
                    layout.ColumnIndex,
                    layout.ColumnCount));
        }
    }

    /// <summary>
    /// 선택 날짜의 같은 일정 데이터를 완료 상태에 따라
    /// '할 일'과 '완료한 일' 목록으로 분리합니다.
    /// 이 목록들은 완료 일정 보기 설정과 관계없이 항상 표시합니다.
    /// </summary>
    private void LoadScheduleLists(IReadOnlyList<ScheduleItem> schedules)
    {
        foreach (var schedule in schedules)
        {
            var scheduleViewModel =
                new DailyScheduleViewModel(
                    schedule,
                    SelectedDate,
                    0,
                    1);

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
    /// 시간이 서로 겹치는 일정들을 그룹으로 묶고
    /// 각 그룹 안에서 사용할 가로 열 번호를 계산합니다.
    /// </summary>
    private IReadOnlyList<DailyScheduleLayout> CreateScheduleLayouts(IReadOnlyList<ScheduleItem> schedules)
    {
        var result = new List<DailyScheduleLayout>();
        var currentGroup = new List<DailyScheduleCandidate>();

        DateTime? currentGroupEndAt = null;

        foreach (var schedule in schedules)
        {
            var candidate = CreateCandidate(schedule);

            if (currentGroup.Count > 0 &&
                currentGroupEndAt.HasValue &&
                candidate.VisibleStartAt >= currentGroupEndAt.Value)
            {
                AddGroupLayouts(
                    currentGroup,
                    result);

                currentGroup.Clear();
                currentGroupEndAt = null;
            }

            currentGroup.Add(candidate);

            if (!currentGroupEndAt.HasValue ||
                candidate.LayoutEndAt > currentGroupEndAt.Value)
            {
                currentGroupEndAt =
                    candidate.LayoutEndAt;
            }
        }

        if (currentGroup.Count > 0)
        {
            AddGroupLayouts(
                currentGroup,
                result);
        }

        return result;
    }

    /// <summary>
    /// 하나의 시간 겹침 그룹에서 사용할 가로 열을 계산합니다.
    /// 먼저 끝난 열은 이후 일정이 다시 사용할 수 있습니다.
    /// </summary>
    private static void AddGroupLayouts(IReadOnlyList<DailyScheduleCandidate> group, ICollection<DailyScheduleLayout> result)
    {
        var columnEndTimes =
            new List<DateTime>();

        var assignments =
            new List<DailyScheduleColumnAssignment>();

        foreach (var candidate in group)
        {
            var columnIndex =
                FindAvailableColumn(
                    columnEndTimes,
                    candidate.VisibleStartAt);

            if (columnIndex ==
                columnEndTimes.Count)
            {
                columnEndTimes.Add(
                    candidate.LayoutEndAt);
            }
            else
            {
                columnEndTimes[columnIndex] =
                    candidate.LayoutEndAt;
            }

            assignments.Add(
                new DailyScheduleColumnAssignment(
                    candidate.Schedule,
                    columnIndex));
        }

        var columnCount =
            columnEndTimes.Count;

        foreach (var assignment in assignments)
        {
            result.Add(
                new DailyScheduleLayout(
                    assignment.Schedule,
                    assignment.ColumnIndex,
                    columnCount));
        }
    }

    /// <summary>
    /// 현재 일정이 사용할 수 있는 가장 왼쪽의 빈 열을 반환합니다.
    /// </summary>
    private static int FindAvailableColumn(IReadOnlyList<DateTime> columnEndTimes, DateTime startAt)
    {
        for (var columnIndex = 0;
             columnIndex < columnEndTimes.Count;
             columnIndex++)
        {
            if (startAt >=
                columnEndTimes[columnIndex])
            {
                return columnIndex;
            }
        }

        return columnEndTimes.Count;
    }

    /// <summary>
    /// 일정의 실제 표시 시간과 최소 카드 높이를 고려한
    /// 겹침 계산 후보를 생성합니다.
    /// </summary>
    private DailyScheduleCandidate CreateCandidate(ScheduleItem schedule)
    {
        var dayEnd =
            SelectedDate.Date.AddDays(1);

        var visibleStartAt =
            GetVisibleStartAt(schedule);

        var visibleEndAt =
            GetVisibleEndAt(schedule);

        var actualDurationMinutes =
            Math.Max(
                0,
                (visibleEndAt -
                 visibleStartAt)
                .TotalMinutes);

        var minimumDisplayMinutes =
            DailyTimelineMetrics.MinimumScheduleHeight /
            DailyTimelineMetrics.HourHeight *
            60.0;

        var layoutDurationMinutes =
            Math.Max(
                actualDurationMinutes,
                minimumDisplayMinutes);

        var layoutEndAt =
            visibleStartAt.AddMinutes(
                layoutDurationMinutes);

        if (layoutEndAt > dayEnd)
        {
            layoutEndAt = dayEnd;
        }

        return new DailyScheduleCandidate(
            schedule,
            visibleStartAt,
            visibleEndAt,
            layoutEndAt);
    }

    /// <summary>
    /// 선택 날짜 안에서 일정이 실제로 보이기 시작하는 시각을 반환합니다.
    /// </summary>
    private DateTime GetVisibleStartAt(ScheduleItem schedule)
    {
        var dayStart =
            SelectedDate.Date;

        return schedule.StartAt < dayStart
            ? dayStart
            : schedule.StartAt;
    }

    /// <summary>
    /// 선택 날짜 안에서 일정이 실제로 보이는 마지막 시각을 반환합니다.
    /// </summary>
    private DateTime GetVisibleEndAt(ScheduleItem schedule)
    {
        var dayEnd =
            SelectedDate.Date.AddDays(1);

        return schedule.EndAt > dayEnd
            ? dayEnd
            : schedule.EndAt;
    }

    /// <summary>
    /// 현재 선택 날짜를 지정한 일수만큼 이동합니다.
    /// </summary>
    private void MoveDay(int dayOffset)
    {
        SelectedDate =
            SelectedDate.AddDays(dayOffset);

        CloseEditor();
        LoadSchedules();
    }

    /// <summary>
    /// 현재 선택 날짜를 실제 오늘 날짜로 되돌립니다.
    /// </summary>
    private void MoveToToday()
    {
        SelectedDate =
            DateTime.Today;

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

    /// <summary>
    /// 실제 화면 배치 전에 사용하는 일간 일정 계산 후보입니다.
    /// </summary>
    private readonly record struct DailyScheduleCandidate(
        ScheduleItem Schedule,
        DateTime VisibleStartAt,
        DateTime VisibleEndAt,
        DateTime LayoutEndAt);

    /// <summary>
    /// 겹침 그룹 안에서 일정 하나에 할당된 가로 열입니다.
    /// </summary>
    private readonly record struct DailyScheduleColumnAssignment(
        ScheduleItem Schedule,
        int ColumnIndex);

    /// <summary>
    /// 일간 일정의 최종 가로 열 배치 결과입니다.
    /// </summary>
    private readonly record struct DailyScheduleLayout(
        ScheduleItem Schedule,
        int ColumnIndex,
        int ColumnCount);
}