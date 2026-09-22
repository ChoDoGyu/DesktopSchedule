using DesktopSchedule.Models;

namespace DesktopSchedule.Services;

/// <summary>
/// DesktopSchedule 자체 설정의 기본값과 영구 저장을 관리합니다.
/// 실제 JSON 파일 읽기와 쓰기는 LocalJsonStore에 위임합니다.
/// </summary>
public sealed class AppSettingsService
{
    private const string FileName = "app-settings.json";

    private readonly LocalJsonStore<AppSettings> _store;

    public AppSettingsService()
    {
        _store = new LocalJsonStore<AppSettings>(FileName);
    }

    /// <summary>
    /// 저장된 애플리케이션 설정을 불러옵니다.
    /// 저장 데이터가 없거나 읽을 수 없는 경우에는 기본 설정을 반환합니다.
    /// </summary>
    public AppSettings Load()
    {
        return _store.Load() ?? CreateDefault();
    }

    /// <summary>
    /// 현재 애플리케이션 설정을 사용자별 로컬 설정 파일에 저장합니다.
    /// </summary>
    public void Save(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        _store.Save(settings);
    }

    /// <summary>
    /// DesktopSchedule의 기본 애플리케이션 설정을 생성합니다.
    /// </summary>
    public AppSettings CreateDefault()
    {
        return new AppSettings
        {
            IsTopmost = false,
            ShowInTaskbar = true
        };
    }
}