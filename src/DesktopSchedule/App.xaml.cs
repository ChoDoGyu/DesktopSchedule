using System.ComponentModel;
using System.Media;
using System.Windows;
using DesktopSchedule.Repositories;
using DesktopSchedule.Services;
using DesktopSchedule.ViewModels;
using DesktopSchedule.Views;
using Forms = System.Windows.Forms;

namespace DesktopSchedule;

/// <summary>
/// 애플리케이션의 시작과 종료 흐름,
/// 시스템 트레이와 일정 알림 생명주기를 관리합니다.
/// </summary>
public partial class App : Application
{
    private static readonly TimeSpan ReminderSoundCooldown = TimeSpan.FromSeconds(1);

    // 애플리케이션 실행 중 하나의 알림 스케줄러를 유지합니다.
    private ReminderScheduler? _reminderScheduler;

    // 화면에서 숨겼다가 다시 표시할 메인 창입니다.
    private MainWindow? _mainWindow;

    // Windows 시스템 트레이에 표시할 아이콘입니다.
    private Forms.NotifyIcon? _trayIcon;

    // 트레이 아이콘 우클릭 메뉴입니다.
    private Forms.ContextMenuStrip? _trayMenu;

    // 트레이의 종료 메뉴를 통해 실제 애플리케이션 종료가 진행 중인지 나타냅니다.
    private bool _isExiting;

    // 거의 동시에 여러 알림이 발생했을 때 알림음이 연속으로 반복되지 않도록
    // 마지막으로 소리를 재생한 시각을 저장합니다.
    private DateTime _lastReminderSoundAt = DateTime.MinValue;

    /// <summary>
    /// 애플리케이션이 시작될 때 데이터베이스, 일정 서비스,
    /// 알림 스케줄러, 시스템 트레이와 메인 창을 준비합니다.
    /// </summary>
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var databaseService = new DatabaseService();
        databaseService.Initialize();

        var scheduleRepository = new SqliteScheduleRepository(databaseService);
        var scheduleService = new ScheduleService(scheduleRepository);

        _reminderScheduler = new ReminderScheduler(scheduleService);
        _reminderScheduler.ReminderDue += ReminderScheduler_ReminderDue;
        _reminderScheduler.Start();

        InitializeTrayIcon();

        var mainWindowViewModel = new MainWindowViewModel(scheduleService);

        _mainWindow = new MainWindow(mainWindowViewModel);
        _mainWindow.Closing += MainWindow_Closing;
        _mainWindow.Show();
    }

    /// <summary>
    /// 시스템 트레이 아이콘과 열기, 종료 메뉴를 생성합니다.
    /// </summary>
    private void InitializeTrayIcon()
    {
        _trayMenu = new Forms.ContextMenuStrip();

        _trayMenu.Items.Add("열기", null, (_, _) => ShowMainWindow());
        _trayMenu.Items.Add("종료", null, (_, _) => ExitApplication());

        _trayIcon = new Forms.NotifyIcon
        {
            Text = "DesktopSchedule",
            Icon = System.Drawing.SystemIcons.Application,
            ContextMenuStrip = _trayMenu,
            Visible = true
        };

        _trayIcon.MouseClick += TrayIcon_MouseClick;
    }

    /// <summary>
    /// 메인 창의 X 버튼을 눌렀을 때 애플리케이션을 종료하지 않고 창만 숨깁니다.
    /// 트레이의 종료 메뉴로 실제 종료 중인 경우에는 정상적으로 창을 닫습니다.
    /// </summary>
    private void MainWindow_Closing(object? sender, CancelEventArgs e)
    {
        if (_isExiting)
        {
            return;
        }

        e.Cancel = true;
        _mainWindow?.Hide();
    }

    /// <summary>
    /// 트레이 아이콘을 왼쪽 클릭하면 숨겨진 메인 창을 다시 표시합니다.
    /// 오른쪽 클릭은 ContextMenuStrip이 처리합니다.
    /// </summary>
    private void TrayIcon_MouseClick(object? sender, Forms.MouseEventArgs e)
    {
        if (e.Button == Forms.MouseButtons.Left)
        {
            ShowMainWindow();
        }
    }

    /// <summary>
    /// 숨겨져 있거나 최소화된 MainWindow를 정상 상태로 복원하고 화면 앞으로 가져옵니다.
    /// </summary>
    private void ShowMainWindow()
    {
        if (_mainWindow is null)
        {
            return;
        }

        _mainWindow.Show();

        if (_mainWindow.WindowState == WindowState.Minimized)
        {
            _mainWindow.WindowState = WindowState.Normal;
        }

        _mainWindow.Activate();
    }

    /// <summary>
    /// 트레이의 종료 메뉴를 선택했을 때 애플리케이션을 완전히 종료합니다.
    /// </summary>
    private void ExitApplication()
    {
        if (_isExiting)
        {
            return;
        }

        _isExiting = true;
        Shutdown();
    }

    /// <summary>
    /// 일정의 알림 시간이 도달하면 기본 알림음을 재생하고 오른쪽 아래에 알림 팝업을 표시합니다.
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
    /// 애플리케이션이 완전히 종료될 때 알림 스케줄러,
    /// 메인 창 이벤트와 시스템 트레이 리소스를 정리합니다.
    /// </summary>
    protected override void OnExit(ExitEventArgs e)
    {
        if (_reminderScheduler is not null)
        {
            _reminderScheduler.ReminderDue -= ReminderScheduler_ReminderDue;
            _reminderScheduler.Stop();
            _reminderScheduler = null;
        }

        if (_mainWindow is not null)
        {
            _mainWindow.Closing -= MainWindow_Closing;
            _mainWindow = null;
        }

        if (_trayIcon is not null)
        {
            _trayIcon.MouseClick -= TrayIcon_MouseClick;
            _trayIcon.Visible = false;
            _trayIcon.Dispose();
            _trayIcon = null;
        }

        if (_trayMenu is not null)
        {
            _trayMenu.Dispose();
            _trayMenu = null;
        }

        base.OnExit(e);
    }
}