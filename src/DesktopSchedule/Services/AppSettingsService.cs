using DesktopSchedule.Models;

namespace DesktopSchedule.Services;

/// <summary>
/// DesktopSchedule 자체 설정의 현재 상태와 영구 저장을 관리합니다.
/// 실제 JSON 파일 읽기와 쓰기는 LocalJsonStore에 위임합니다.
/// </summary>
public sealed class AppSettingsService
{
    private const string FileName = "app-settings.json";

    private readonly LocalJsonStore<AppSettings> _store;
    private AppSettings _settings;

    /// <summary>
    /// MainWindow를 다른 프로그램 창보다 항상 위에 표시할지 여부입니다.
    /// </summary>
    public bool IsTopmost => _settings.IsTopmost;

    /// <summary>
    /// MainWindow를 Windows 작업 표시줄에 표시할지 여부입니다.
    /// </summary>
    public bool ShowInTaskbar => _settings.ShowInTaskbar;

    /// <summary>
    /// 애플리케이션 설정이 변경되고 저장까지 완료되었을 때 발생합니다.
    /// </summary>
    public event Action? SettingsChanged;

    public AppSettingsService()
    {
        _store = new LocalJsonStore<AppSettings>(FileName);
        _settings = _store.Load() ?? CreateDefault();
    }

    /// <summary>
    /// 항상 위 표시 설정을 변경하고 즉시 저장합니다.
    /// 현재 값과 같으면 저장하지 않습니다.
    /// </summary>
    public void SetIsTopmost(bool value)
    {
        if (_settings.IsTopmost == value)
        {
            return;
        }

        Save(new AppSettings
        {
            IsTopmost = value,
            ShowInTaskbar = _settings.ShowInTaskbar
        });
    }

    /// <summary>
    /// 작업 표시줄 표시 설정을 변경하고 즉시 저장합니다.
    /// 현재 값과 같으면 저장하지 않습니다.
    /// </summary>
    public void SetShowInTaskbar(bool value)
    {
        if (_settings.ShowInTaskbar == value)
        {
            return;
        }

        Save(new AppSettings
        {
            IsTopmost = _settings.IsTopmost,
            ShowInTaskbar = value
        });
    }

    /// <summary>
    /// DesktopSchedule 자체 설정을 기본값으로 되돌리고 즉시 저장합니다.
    /// 이미 기본값이라면 다시 저장하지 않습니다.
    /// </summary>
    public void ResetToDefault()
    {
        var defaultSettings = CreateDefault();

        if (_settings.IsTopmost == defaultSettings.IsTopmost && _settings.ShowInTaskbar == defaultSettings.ShowInTaskbar)
        {
            return;
        }

        Save(defaultSettings);
    }

    /// <summary>
    /// 새 설정을 JSON 파일에 저장한 뒤 현재 상태를 교체하고 변경을 알립니다.
    /// 저장에 실패한 경우 기존 메모리 상태는 유지합니다.
    /// </summary>
    private void Save(AppSettings settings)
    {
        _store.Save(settings);
        _settings = settings;
        SettingsChanged?.Invoke();
    }

    /// <summary>
    /// DesktopSchedule의 기본 애플리케이션 설정을 생성합니다.
    /// </summary>
    private static AppSettings CreateDefault()
    {
        return new AppSettings
        {
            IsTopmost = false,
            ShowInTaskbar = true
        };
    }
}