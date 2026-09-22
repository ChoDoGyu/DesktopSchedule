using System.IO;
using DesktopSchedule.Commands;
using DesktopSchedule.Services;

namespace DesktopSchedule.ViewModels;

/// <summary>
/// 애플리케이션 설정 화면의 상태와 동작을 관리합니다.
/// </summary>
public class SettingsViewModel : ViewModelBase
{
    private readonly StartupService _startupService;
    private readonly AppSettingsService _appSettingsService;

    private bool _isStartupEnabled;
    private string _startupErrorMessage = string.Empty;
    private string _appSettingsErrorMessage = string.Empty;

    /// <summary>
    /// 창 위치와 크기를 기본값으로 되돌려야 할 때 발생합니다.
    /// 실제 Window 처리는 상위 애플리케이션 흐름에 위임합니다.
    /// </summary>
    public event Action? WindowResetRequested;

    /// <summary>
    /// Windows 로그인 시 DesktopSchedule을 자동으로 실행할지 여부입니다.
    /// 실제 Windows 시작 프로그램 등록 상태와 직접 연결됩니다.
    /// </summary>
    public bool IsStartupEnabled
    {
        get => _isStartupEnabled;
        set
        {
            if (_isStartupEnabled == value)
            {
                return;
            }

            try
            {
                if (value)
                {
                    _startupService.Enable();
                }
                else
                {
                    _startupService.Disable();
                }

                SetProperty(ref _isStartupEnabled, value);
                StartupErrorMessage = string.Empty;
            }
            catch (Exception exception)
            {
                StartupErrorMessage = $"시작 프로그램 설정을 변경하지 못했습니다. {exception.Message}";
                OnPropertyChanged(nameof(IsStartupEnabled));
            }
        }
    }

    /// <summary>
    /// MainWindow를 다른 프로그램보다 항상 위에 표시할지 여부입니다.
    /// 변경하면 즉시 저장되고 현재 MainWindow에도 반영됩니다.
    /// </summary>
    public bool IsTopmost
    {
        get => _appSettingsService.IsTopmost;
        set
        {
            if (_appSettingsService.IsTopmost == value)
            {
                return;
            }

            ApplyAppSetting(() => _appSettingsService.SetIsTopmost(value), "설정을 저장하지 못했습니다.", nameof(IsTopmost));
        }
    }

    /// <summary>
    /// MainWindow를 Windows 작업 표시줄에 표시할지 여부입니다.
    /// 변경하면 즉시 저장되고 현재 MainWindow에도 반영됩니다.
    /// </summary>
    public bool ShowInTaskbar
    {
        get => _appSettingsService.ShowInTaskbar;
        set
        {
            if (_appSettingsService.ShowInTaskbar == value)
            {
                return;
            }

            ApplyAppSetting(() => _appSettingsService.SetShowInTaskbar(value), "설정을 저장하지 못했습니다.", nameof(ShowInTaskbar));
        }
    }

    /// <summary>
    /// Windows 시작 프로그램 설정 변경에 실패했을 때 표시할 오류 메시지입니다.
    /// </summary>
    public string StartupErrorMessage
    {
        get => _startupErrorMessage;
        private set => SetProperty(ref _startupErrorMessage, value);
    }

    /// <summary>
    /// 애플리케이션 설정 저장 또는 초기화에 실패했을 때 표시할 오류 메시지입니다.
    /// </summary>
    public string AppSettingsErrorMessage
    {
        get => _appSettingsErrorMessage;
        private set => SetProperty(ref _appSettingsErrorMessage, value);
    }

    /// <summary>
    /// 앱 표시 설정과 MainWindow 위치 및 크기를 최초 기본값으로 되돌립니다.
    /// Windows 시작 프로그램 설정과 일정 데이터는 변경하지 않습니다.
    /// </summary>
    public RelayCommand ResetSettingsCommand { get; }

    public SettingsViewModel(StartupService startupService, AppSettingsService appSettingsService)
    {
        _startupService = startupService ?? throw new ArgumentNullException(nameof(startupService));
        _appSettingsService = appSettingsService ?? throw new ArgumentNullException(nameof(appSettingsService));

        ResetSettingsCommand = new RelayCommand(_ => ResetSettings());

        LoadStartupState();
    }

    /// <summary>
    /// 현재 Windows 시작 프로그램에 DesktopSchedule이 정상 등록되어 있는지 읽어옵니다.
    /// </summary>
    private void LoadStartupState()
    {
        try
        {
            _isStartupEnabled = _startupService.IsEnabled();
            StartupErrorMessage = string.Empty;
        }
        catch (Exception exception)
        {
            _isStartupEnabled = false;
            StartupErrorMessage = $"시작 프로그램 상태를 확인하지 못했습니다. {exception.Message}";
        }
    }

    /// <summary>
    /// 앱 표시 설정을 기본값으로 저장한 뒤 창 위치와 크기 초기화를 요청합니다.
    /// </summary>
    private void ResetSettings()
    {
        ApplyAppSetting(() =>
        {
            _appSettingsService.ResetToDefault();
            WindowResetRequested?.Invoke();
        }, "설정을 초기화하지 못했습니다.", nameof(IsTopmost), nameof(ShowInTaskbar));
    }

    /// <summary>
    /// 애플리케이션 설정 변경에 필요한 저장 예외 처리와
    /// ViewModel 속성 갱신을 공통으로 수행합니다.
    /// </summary>
    private void ApplyAppSetting(Action action, string errorMessage, params string[] propertyNames)
    {
        try
        {
            action();
            AppSettingsErrorMessage = string.Empty;
        }
        catch (IOException exception)
        {
            AppSettingsErrorMessage = $"{errorMessage} {exception.Message}";
        }
        catch (UnauthorizedAccessException exception)
        {
            AppSettingsErrorMessage = $"{errorMessage} {exception.Message}";
        }

        foreach (var propertyName in propertyNames)
        {
            OnPropertyChanged(propertyName);
        }
    }
}