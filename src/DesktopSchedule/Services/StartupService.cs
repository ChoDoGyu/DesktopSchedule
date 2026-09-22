using Microsoft.Win32;

namespace DesktopSchedule.Services;

/// <summary>
/// 현재 Windows 사용자의 시작 프로그램에 DesktopSchedule을 등록하거나 해제합니다.
/// HKEY_CURRENT_USER의 Run 항목을 사용하므로 관리자 권한이 필요하지 않습니다.
/// </summary>
public sealed class StartupService
{
    private const string RegistryPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string RegistryValueName = "DesktopSchedule";

    /// <summary>
    /// DesktopSchedule이 현재 Windows 사용자의 시작 프로그램에 등록되어 있는지 확인합니다.
    /// </summary>
    public bool IsEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RegistryPath, false);

        if (key is null)
        {
            return false;
        }

        var registeredCommand = key.GetValue(RegistryValueName) as string;
        var expectedCommand = GetStartupCommand();

        return string.Equals(
            registeredCommand,
            expectedCommand,
            StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// DesktopSchedule을 현재 Windows 사용자의 시작 프로그램에 등록합니다.
    /// </summary>
    public void Enable()
    {
        using var key = Registry.CurrentUser.CreateSubKey(RegistryPath, true);

        if (key is null)
        {
            throw new InvalidOperationException("Windows 시작 프로그램 등록 위치를 열 수 없습니다.");
        }

        key.SetValue(
            RegistryValueName,
            GetStartupCommand(),
            RegistryValueKind.String);
    }

    /// <summary>
    /// DesktopSchedule의 시작 프로그램 등록을 제거합니다.
    /// 등록되어 있지 않은 경우에는 아무 작업도 하지 않습니다.
    /// </summary>
    public void Disable()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RegistryPath, true);

        if (key is null)
        {
            return;
        }

        key.DeleteValue(RegistryValueName, false);
    }

    /// <summary>
    /// Windows 시작 프로그램에 저장할 현재 실행 파일 명령을 반환합니다.
    /// 경로에 공백이 포함될 수 있으므로 큰따옴표로 감싸서 저장합니다.
    /// </summary>
    private static string GetStartupCommand()
    {
        var executablePath = Environment.ProcessPath;

        if (string.IsNullOrWhiteSpace(executablePath))
        {
            throw new InvalidOperationException("현재 DesktopSchedule 실행 파일 경로를 확인할 수 없습니다.");
        }

        return $"\"{executablePath}\"";
    }
}