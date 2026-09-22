using System.IO;
using System.Media;
using System.Windows;
using DesktopSchedule.Repositories;
using DesktopSchedule.Services;
using DesktopSchedule.ViewModels;
using DesktopSchedule.Views;

namespace DesktopSchedule;

/// <summary>
/// 애플리케이션의 시작과 종료 흐름과
/// 핵심 서비스들의 생명주기를 관리합니다.
/// </summary>
public partial class App : Application
{
    private static readonly TimeSpan ReminderSoundCooldown = TimeSpan.FromSeconds(1);

    private ReminderScheduler? _reminderScheduler;
    private WindowPlacementService? _windowPlacementService;
    private TrayIconService? _trayIconService;
    private MainWindow? _mainWindow;

    private DateTime _lastReminderSoundAt = DateTime.MinValue;

    /// <summary>
    /// 애플리케이션이 시작될 때 데이터베이스, 일정 서비스,
    /// Windows 연동 서비스, 애플리케이션 설정, 알림 스케줄러,
    /// 메인 창과 시스템 트레이를 준비합니다.
    /// </summary>
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var databaseService = new DatabaseService();
        databaseService.Initialize();

        var scheduleRepository = new SqliteScheduleRepository(databaseService);
        var scheduleService = new ScheduleService(scheduleRepository);
        var startupService = new StartupService();
        var appSettingsService = new AppSettingsService();

        _windowPlacementService = new WindowPlacementService();

        _reminderScheduler = new ReminderScheduler(scheduleService);
        _reminderScheduler.ReminderDue += ReminderScheduler_ReminderDue;
        _reminderScheduler.Start();

        var mainWindowViewModel = new MainWindowViewModel(scheduleService, startupService, appSettingsService, _windowPlacementService);

        _mainWindow = new MainWindow(mainWindowViewModel);

        _windowPlacementService.Restore(_mainWindow);

        _trayIconService = new TrayIconService(_mainWindow, Shutdown);

        _mainWindow.Show();
    }

    /// <summary>
    /// 일정의 알림 시간이 도달하면 기본 알림음을 재생하고
    /// 화면 오른쪽 아래에 알림 팝업을 표시합니다.
    /// 여러 알림이 거의 동시에 발생한 경우 팝업은 모두 표시하되 알림음은 한 번만 재생합니다.
    /// </summary>
    private void ReminderScheduler_ReminderDue(object? sender, ReminderDueEventArgs e)
    {
        var now = DateTime.Now;

        if (now - _lastReminderSoundAt >= ReminderSoundCooldown)
        {
            SystemSounds.Asterisk.Play();
            _lastReminderSoundAt = now;
        }

        var popupWindow = new ReminderPopupWindow(e.Schedule);
        popupWindow.Show();
    }

    /// <summary>
    /// 애플리케이션이 완전히 종료될 때 현재 MainWindow의 마지막 정상 위치와 크기를 저장하고,
    /// 알림 스케줄러와 시스템 트레이 리소스를 정리합니다.
    /// </summary>
    protected override void OnExit(ExitEventArgs e)
    {
        if (_mainWindow is not null && _windowPlacementService is not null)
        {
            try
            {
                _windowPlacementService.Save(_mainWindow);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }

        if (_reminderScheduler is not null)
        {
            _reminderScheduler.ReminderDue -= ReminderScheduler_ReminderDue;
            _reminderScheduler.Stop();
            _reminderScheduler = null;
        }

        _trayIconService?.Dispose();
        _trayIconService = null;

        _mainWindow = null;
        _windowPlacementService = null;

        base.OnExit(e);
    }
}