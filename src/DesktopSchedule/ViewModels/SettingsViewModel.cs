using DesktopSchedule.Services;

namespace DesktopSchedule.ViewModels;

/// <summary>
/// 애플리케이션 설정 화면의 상태와 동작을 관리합니다.
/// </summary>
public class SettingsViewModel : ViewModelBase
{
    private readonly StartupService _startupService;
    private readonly MainWindowViewModel _mainWindowViewModel;

    private bool _isStartupEnabled;
    private string _startupErrorMessage = string.Empty;

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
    /// 변경하면 현재 실행 중인 MainWindow에 즉시 반영됩니다.
    /// </summary>
    public bool IsTopmost
    {
        get => _mainWindowViewModel.IsTopmost;
        set
        {
            if (_mainWindowViewModel.IsTopmost == value)
            {
                return;
            }

            _mainWindowViewModel.IsTopmost = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// MainWindow를 Windows 작업 표시줄에 표시할지 여부입니다.
    /// 시스템 트레이 아이콘 표시에는 영향을 주지 않습니다.
    /// </summary>
    public bool ShowInTaskbar
    {
        get => _mainWindowViewModel.ShowInTaskbar;
        set
        {
            if (_mainWindowViewModel.ShowInTaskbar == value)
            {
                return;
            }

            _mainWindowViewModel.ShowInTaskbar = value;
            OnPropertyChanged();
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

    public SettingsViewModel(StartupService startupService, MainWindowViewModel mainWindowViewModel)
    {
        _startupService = startupService ?? throw new ArgumentNullException(nameof(startupService));
        _mainWindowViewModel = mainWindowViewModel ?? throw new ArgumentNullException(nameof(mainWindowViewModel));

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
}