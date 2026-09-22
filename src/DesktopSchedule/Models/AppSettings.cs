namespace DesktopSchedule.Models;

/// <summary>
/// DesktopSchedule 자체에서 영구적으로 저장하는 애플리케이션 설정을 나타냅니다.
/// Windows 자체에서 관리하는 시작 프로그램 등록 상태나 창 위치 정보는 포함하지 않습니다.
/// </summary>
public sealed class AppSettings
{
    /// <summary>
    /// MainWindow를 다른 프로그램 창보다 항상 위에 표시할지 여부입니다.
    /// </summary>
    public bool IsTopmost { get; set; }

    /// <summary>
    /// MainWindow를 Windows 작업 표시줄에 표시할지 여부입니다.
    /// </summary>
    public bool ShowInTaskbar { get; set; } = true;
}