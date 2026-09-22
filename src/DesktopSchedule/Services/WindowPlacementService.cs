using System.Windows;
using Forms = System.Windows.Forms;

namespace DesktopSchedule.Services;

/// <summary>
/// DesktopSchedule 메인 창의 마지막 정상 위치와 크기를 저장하고 복원합니다.
/// 다중 모니터 환경에서 저장된 위치가 현재 모든 화면 밖에 있다면
/// 주 모니터의 작업 영역 안으로 안전하게 복구합니다.
/// </summary>
public sealed class WindowPlacementService
{
    private const string FileName = "window-placement.json";
    private const double MinimumVisibleWidth = 120.0;
    private const double MinimumVisibleHeight = 80.0;

    public const double DefaultWidth = 1120.0;
    public const double DefaultHeight = 960.0;

    private readonly LocalJsonStore<WindowPlacement> _store;

    public WindowPlacementService()
    {
        _store = new LocalJsonStore<WindowPlacement>(FileName);
    }

    /// <summary>
    /// 현재 MainWindow의 마지막 정상 위치와 크기를 저장합니다.
    /// 최소화 또는 최대화 상태라면 RestoreBounds를 사용합니다.
    /// </summary>
    public void Save(Window window)
    {
        ArgumentNullException.ThrowIfNull(window);

        var bounds = GetNormalBounds(window);

        if (!IsValidBounds(bounds))
        {
            return;
        }

        var placement = new WindowPlacement
        {
            Left = bounds.Left,
            Top = bounds.Top,
            Width = bounds.Width,
            Height = bounds.Height
        };

        _store.Save(placement);
    }

    /// <summary>
    /// 저장된 MainWindow 위치와 크기를 불러와 창에 적용합니다.
    /// 저장 위치가 현재 연결된 모든 모니터 밖에 있다면 주 모니터 중앙으로 복구합니다.
    /// 저장 정보가 없거나 읽을 수 없는 경우 기존 XAML의 기본 위치와 크기를 그대로 사용합니다.
    /// </summary>
    public void Restore(Window window)
    {
        ArgumentNullException.ThrowIfNull(window);

        var placement = _store.Load();

        if (placement is null)
        {
            return;
        }

        var width = Math.Max(placement.Width, window.MinWidth);
        var height = Math.Max(placement.Height, window.MinHeight);
        var savedBounds = new Rect(placement.Left, placement.Top, width, height);

        if (!IsValidBounds(savedBounds))
        {
            return;
        }

        window.WindowStartupLocation = WindowStartupLocation.Manual;
        window.Width = width;
        window.Height = height;

        if (IsVisibleOnAnyScreen(savedBounds))
        {
            window.Left = placement.Left;
            window.Top = placement.Top;
            return;
        }

        MoveToPrimaryScreenCenter(window, width, height);
    }

    /// <summary>
    /// 저장된 창 위치와 크기를 삭제하고 전달받은 Window를
    /// 최초 실행 기본 크기와 주 모니터 중앙 위치로 즉시 되돌립니다.
    /// </summary>
    public void ResetToDefault(Window window)
    {
        ArgumentNullException.ThrowIfNull(window);

        _store.Delete();

        window.WindowState = WindowState.Normal;
        window.WindowStartupLocation = WindowStartupLocation.Manual;
        MoveToPrimaryScreenCenter(window, DefaultWidth, DefaultHeight);
    }

    /// <summary>
    /// 저장된 창 영역이 현재 연결된 모니터 중 하나에 충분히 표시되는지 확인합니다.
    /// 아주 작은 일부만 화면에 걸쳐 있는 경우에는 정상 위치로 인정하지 않습니다.
    /// </summary>
    private static bool IsVisibleOnAnyScreen(Rect windowBounds)
    {
        foreach (var screen in Forms.Screen.AllScreens)
        {
            var workingArea = screen.WorkingArea;
            var screenBounds = new Rect(workingArea.Left, workingArea.Top, workingArea.Width, workingArea.Height);
            var intersection = Rect.Intersect(windowBounds, screenBounds);

            if (!intersection.IsEmpty && intersection.Width >= MinimumVisibleWidth && intersection.Height >= MinimumVisibleHeight)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 지정된 크기로 MainWindow를 조정한 뒤 주 모니터 작업 영역의 중앙으로 이동합니다.
    /// 창 크기가 작업 영역보다 큰 경우에는 가능한 범위 안으로 크기를 제한합니다.
    /// </summary>
    private static void MoveToPrimaryScreenCenter(Window window, double width, double height)
    {
        var primaryScreen = Forms.Screen.PrimaryScreen;

        if (primaryScreen is null)
        {
            var workingArea = SystemParameters.WorkArea;
            var restoredWidth = Math.Min(width, Math.Max(window.MinWidth, workingArea.Width));
            var restoredHeight = Math.Min(height, Math.Max(window.MinHeight, workingArea.Height));

            window.Width = restoredWidth;
            window.Height = restoredHeight;
            window.Left = workingArea.Left + (workingArea.Width - restoredWidth) / 2.0;
            window.Top = workingArea.Top + (workingArea.Height - restoredHeight) / 2.0;
            return;
        }

        var primaryWorkingArea = primaryScreen.WorkingArea;
        var primaryRestoredWidth = Math.Min(width, Math.Max(window.MinWidth, primaryWorkingArea.Width));
        var primaryRestoredHeight = Math.Min(height, Math.Max(window.MinHeight, primaryWorkingArea.Height));

        window.Width = primaryRestoredWidth;
        window.Height = primaryRestoredHeight;
        window.Left = primaryWorkingArea.Left + (primaryWorkingArea.Width - primaryRestoredWidth) / 2.0;
        window.Top = primaryWorkingArea.Top + (primaryWorkingArea.Height - primaryRestoredHeight) / 2.0;
    }

    /// <summary>
    /// 창이 정상 상태라면 현재 영역을 사용하고,
    /// 최소화 또는 최대화 상태라면 정상 상태로 돌아왔을 때 사용할 영역을 반환합니다.
    /// </summary>
    private static Rect GetNormalBounds(Window window)
    {
        if (window.WindowState == WindowState.Normal)
        {
            return new Rect(window.Left, window.Top, window.Width, window.Height);
        }

        return window.RestoreBounds;
    }

    /// <summary>
    /// 저장하거나 복원할 창 영역이 유효한 좌표와 크기를 가지고 있는지 확인합니다.
    /// </summary>
    private static bool IsValidBounds(Rect bounds)
    {
        return double.IsFinite(bounds.Left) &&
               double.IsFinite(bounds.Top) &&
               double.IsFinite(bounds.Width) &&
               double.IsFinite(bounds.Height) &&
               bounds.Width > 0 &&
               bounds.Height > 0;
    }

    /// <summary>
    /// 창 위치 저장 파일에 기록되는 내부 데이터 형식입니다.
    /// WindowPlacementService 외부에서는 사용할 필요가 없으므로 비공개로 유지합니다.
    /// </summary>
    private sealed class WindowPlacement
    {
        public double Left { get; set; }
        public double Top { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
    }
}