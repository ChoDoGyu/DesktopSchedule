using System.ComponentModel;
using System.Windows;
using Forms = System.Windows.Forms;

namespace DesktopSchedule.Services;

/// <summary>
/// DesktopSchedule의 시스템 트레이 아이콘과 MainWindow 표시 상태를 관리합니다.
/// X 버튼은 창 숨김으로 처리하고 트레이의 종료 메뉴를 통해서만 실제 종료를 요청합니다.
/// </summary>
public sealed class TrayIconService : IDisposable
{
    private const string TrayIconResourcePath = "pack://application:,,,/Resources/Icons/DesktopSchedule.ico";

    private readonly Window _mainWindow;
    private readonly Action _exitApplication;
    private readonly Forms.NotifyIcon _trayIcon;
    private readonly Forms.ContextMenuStrip _trayMenu;
    private readonly Forms.ToolStripMenuItem _trayWindowMenuItem;
    private readonly System.Drawing.Icon _trayIconImage;

    private bool _isExiting;
    private bool _isDisposed;

    public TrayIconService(Window mainWindow, Action exitApplication)
    {
        _mainWindow = mainWindow ?? throw new ArgumentNullException(nameof(mainWindow));
        _exitApplication = exitApplication ?? throw new ArgumentNullException(nameof(exitApplication));

        _trayIconImage = LoadTrayIcon();

        _trayMenu = new Forms.ContextMenuStrip();

        _trayWindowMenuItem = new Forms.ToolStripMenuItem("열기");
        _trayWindowMenuItem.Click += TrayWindowMenuItem_Click;

        _trayMenu.Items.Add(_trayWindowMenuItem);
        _trayMenu.Items.Add(new Forms.ToolStripSeparator());
        _trayMenu.Items.Add("종료", null, (_, _) => ExitApplication());

        _trayMenu.Opening += TrayMenu_Opening;

        _trayIcon = new Forms.NotifyIcon
        {
            Text = "DesktopSchedule",
            Icon = _trayIconImage,
            ContextMenuStrip = _trayMenu,
            Visible = true
        };

        _trayIcon.MouseClick += TrayIcon_MouseClick;

        _mainWindow.Closing += MainWindow_Closing;
        _mainWindow.IsVisibleChanged += MainWindow_IsVisibleChanged;
        _mainWindow.StateChanged += MainWindow_StateChanged;

        UpdateWindowMenuText();
    }

    /// <summary>
    /// 애플리케이션 리소스에 포함된 DesktopSchedule 전용 트레이 아이콘을 불러옵니다.
    /// 원본 Stream과 독립적으로 사용할 수 있도록 Icon을 복제하여 반환합니다.
    /// </summary>
    private static System.Drawing.Icon LoadTrayIcon()
    {
        var resourceUri = new Uri(TrayIconResourcePath, UriKind.Absolute);
        var resourceInfo = Application.GetResourceStream(resourceUri);

        if (resourceInfo is null)
        {
            throw new InvalidOperationException("트레이 아이콘 리소스를 찾을 수 없습니다.");
        }

        using var stream = resourceInfo.Stream;
        using var icon = new System.Drawing.Icon(stream);

        return (System.Drawing.Icon)icon.Clone();
    }

    /// <summary>
    /// MainWindow의 X 버튼을 눌렀을 때 실제 종료 대신 창만 숨깁니다.
    /// 트레이의 종료 메뉴로 종료가 진행 중인 경우에는 정상적으로 닫히도록 허용합니다.
    /// </summary>
    private void MainWindow_Closing(object? sender, CancelEventArgs e)
    {
        if (_isExiting)
        {
            return;
        }

        e.Cancel = true;
        HideMainWindow();
    }

    /// <summary>
    /// MainWindow 표시 여부가 변경되면 트레이 메뉴 상태를 갱신합니다.
    /// </summary>
    private void MainWindow_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        UpdateWindowMenuText();
    }

    /// <summary>
    /// MainWindow가 최소화되거나 정상 상태로 복원되면 트레이 메뉴 상태를 갱신합니다.
    /// </summary>
    private void MainWindow_StateChanged(object? sender, EventArgs e)
    {
        UpdateWindowMenuText();
    }

    /// <summary>
    /// 트레이 아이콘을 왼쪽 클릭하면 MainWindow를 표시하고 화면 앞으로 가져옵니다.
    /// </summary>
    private void TrayIcon_MouseClick(object? sender, Forms.MouseEventArgs e)
    {
        if (e.Button == Forms.MouseButtons.Left)
        {
            ShowMainWindow();
        }
    }

    /// <summary>
    /// 트레이의 열기/숨기기 메뉴를 현재 MainWindow 상태에 맞게 실행합니다.
    /// </summary>
    private void TrayWindowMenuItem_Click(object? sender, EventArgs e)
    {
        if (IsMainWindowDisplayed())
        {
            HideMainWindow();
            return;
        }

        ShowMainWindow();
    }

    /// <summary>
    /// 트레이 메뉴가 열리기 직전에 실제 MainWindow 상태를 다시 반영합니다.
    /// </summary>
    private void TrayMenu_Opening(object? sender, CancelEventArgs e)
    {
        UpdateWindowMenuText();
    }

    /// <summary>
    /// 숨겨지거나 최소화된 MainWindow를 정상 상태로 복원하고 앞으로 가져옵니다.
    /// 이미 정상적으로 표시되어 있다면 기존 창만 활성화합니다.
    /// </summary>
    private void ShowMainWindow()
    {
        if (!_mainWindow.IsVisible)
        {
            _mainWindow.Show();
        }

        if (_mainWindow.WindowState == WindowState.Minimized)
        {
            _mainWindow.WindowState = WindowState.Normal;
        }

        _mainWindow.Activate();

        UpdateWindowMenuText();
    }

    /// <summary>
    /// MainWindow만 숨기며 DesktopSchedule 프로세스와 백그라운드 알림은 유지합니다.
    /// </summary>
    private void HideMainWindow()
    {
        if (!_mainWindow.IsVisible)
        {
            return;
        }

        _mainWindow.Hide();

        UpdateWindowMenuText();
    }

    /// <summary>
    /// MainWindow가 화면에 정상 상태로 표시되어 있는지 확인합니다.
    /// 최소화 상태는 화면에 표시되지 않은 상태로 취급합니다.
    /// </summary>
    private bool IsMainWindowDisplayed()
    {
        return _mainWindow.IsVisible &&
               _mainWindow.WindowState != WindowState.Minimized;
    }

    /// <summary>
    /// MainWindow 상태에 따라 트레이 메뉴를 열기 또는 숨기기로 표시합니다.
    /// </summary>
    private void UpdateWindowMenuText()
    {
        _trayWindowMenuItem.Text = IsMainWindowDisplayed() ? "숨기기" : "열기";
    }

    /// <summary>
    /// 트레이 종료 메뉴를 선택하면 창 숨김 처리를 해제하고 애플리케이션 종료를 요청합니다.
    /// </summary>
    private void ExitApplication()
    {
        if (_isExiting)
        {
            return;
        }

        _isExiting = true;
        _exitApplication();
    }

    /// <summary>
    /// 트레이 아이콘과 MainWindow 이벤트 연결을 모두 정리합니다.
    /// </summary>
    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;

        _mainWindow.Closing -= MainWindow_Closing;
        _mainWindow.IsVisibleChanged -= MainWindow_IsVisibleChanged;
        _mainWindow.StateChanged -= MainWindow_StateChanged;

        _trayIcon.MouseClick -= TrayIcon_MouseClick;
        _trayIcon.Visible = false;
        _trayIcon.Dispose();

        _trayIconImage.Dispose();

        _trayWindowMenuItem.Click -= TrayWindowMenuItem_Click;

        _trayMenu.Opening -= TrayMenu_Opening;
        _trayMenu.Dispose();
    }
}