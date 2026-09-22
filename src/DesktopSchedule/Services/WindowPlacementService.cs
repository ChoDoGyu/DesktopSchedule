using System.IO;
using System.Text.Json;
using System.Windows;
using Forms = System.Windows.Forms;

namespace DesktopSchedule.Services;

/// <summary>
/// DesktopSchedule 메인 창의 마지막 정상 위치와 크기를 로컬 파일에 저장하고 복원합니다.
/// 다중 모니터 환경에서 저장된 위치가 현재 모든 화면 밖에 있다면
/// 주 모니터의 작업 영역 안으로 안전하게 복구합니다.
/// </summary>
public sealed class WindowPlacementService
{
    private const string FileName = "window-placement.json";

    // 창이 화면에 정상적으로 존재한다고 판단하기 위해 필요한 최소 표시 영역입니다.
    private const double MinimumVisibleWidth = 120.0;
    private const double MinimumVisibleHeight = 80.0;

    private readonly string _filePath;

    public WindowPlacementService()
    {
        var localAppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var dataDirectoryPath = Path.Combine(localAppDataPath, "DesktopSchedule");

        _filePath = Path.Combine(dataDirectoryPath, FileName);
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

        var directoryPath = Path.GetDirectoryName(_filePath);

        if (string.IsNullOrWhiteSpace(directoryPath))
        {
            throw new InvalidOperationException("창 위치 설정 파일의 저장 경로를 확인할 수 없습니다.");
        }

        Directory.CreateDirectory(directoryPath);

        var json = JsonSerializer.Serialize(
            placement,
            new JsonSerializerOptions
            {
                WriteIndented = true
            });

        File.WriteAllText(_filePath, json);
    }

    /// <summary>
    /// 저장된 MainWindow 위치와 크기를 불러와 창에 적용합니다.
    /// 저장 위치가 현재 연결된 모든 모니터 밖에 있다면 주 모니터 중앙으로 복구합니다.
    /// 저장 정보가 없거나 읽을 수 없는 경우 기존 XAML의 기본 위치와 크기를 그대로 사용합니다.
    /// </summary>
    public void Restore(Window window)
    {
        ArgumentNullException.ThrowIfNull(window);

        var placement = Load();

        if (placement is null)
        {
            return;
        }

        var width = Math.Max(placement.Width, window.MinWidth);
        var height = Math.Max(placement.Height, window.MinHeight);

        var savedBounds = new Rect(
            placement.Left,
            placement.Top,
            width,
            height);

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
    /// 저장 파일에서 MainWindow 위치와 크기를 읽습니다.
    /// 파일이 없거나 손상되었거나 접근할 수 없으면 null을 반환합니다.
    /// </summary>
    private WindowPlacement? Load()
    {
        if (!File.Exists(_filePath))
        {
            return null;
        }

        try
        {
            var json = File.ReadAllText(_filePath);
            return JsonSerializer.Deserialize<WindowPlacement>(json);
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
        catch (JsonException)
        {
            return null;
        }
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

            var screenBounds = new Rect(
                workingArea.Left,
                workingArea.Top,
                workingArea.Width,
                workingArea.Height);

            var intersection = Rect.Intersect(windowBounds, screenBounds);

            if (!intersection.IsEmpty &&
                intersection.Width >= MinimumVisibleWidth &&
                intersection.Height >= MinimumVisibleHeight)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 저장된 위치를 사용할 수 없을 때 MainWindow를 주 모니터 작업 영역의 중앙으로 이동합니다.
    /// 창 크기가 작업 영역보다 큰 경우에는 가능한 범위 안으로 크기를 제한합니다.
    /// </summary>
    private static void MoveToPrimaryScreenCenter(Window window, double width, double height)
    {
        var primaryScreen = Forms.Screen.PrimaryScreen;

        if (primaryScreen is null)
        {
            window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            return;
        }

        var workingArea = primaryScreen.WorkingArea;

        var restoredWidth = Math.Min(
            width,
            Math.Max(window.MinWidth, workingArea.Width));

        var restoredHeight = Math.Min(
            height,
            Math.Max(window.MinHeight, workingArea.Height));

        window.Width = restoredWidth;
        window.Height = restoredHeight;

        window.Left = workingArea.Left + (workingArea.Width - restoredWidth) / 2.0;
        window.Top = workingArea.Top + (workingArea.Height - restoredHeight) / 2.0;
    }

    /// <summary>
    /// 창이 정상 상태라면 현재 영역을 사용하고,
    /// 최소화 또는 최대화 상태라면 정상 상태로 돌아왔을 때 사용할 영역을 반환합니다.
    /// </summary>
    private static Rect GetNormalBounds(Window window)
    {
        if (window.WindowState == WindowState.Normal)
        {
            return new Rect(
                window.Left,
                window.Top,
                window.Width,
                window.Height);
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
}

/// <summary>
/// 저장 파일에 기록되는 MainWindow 위치와 크기 데이터입니다.
/// </summary>
public sealed class WindowPlacement
{
    public double Left { get; set; }

    public double Top { get; set; }

    public double Width { get; set; }

    public double Height { get; set; }
}