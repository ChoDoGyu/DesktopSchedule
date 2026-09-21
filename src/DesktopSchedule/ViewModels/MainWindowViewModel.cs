namespace DesktopSchedule.ViewModels; // MainWindow에서 사용할 ViewModel이 속하는 네임스페이스입니다.

/// <summary>
/// MainWindow에 표시되는 상태와 동작을 관리합니다.
/// </summary>
public class MainWindowViewModel : ViewModelBase
{
    // 화면에 표시할 제목의 실제 값을 저장하는 필드입니다.
    private string _title = "DesktopSchedule";

    /// <summary>
    /// MainWindow에 표시할 제목입니다.
    /// </summary>
    public string Title
    {
        // 현재 _title 값을 반환합니다.
        get => _title;

        // 새로운 값이 들어오면 ViewModelBase의 SetProperty를 통해
        // 값을 변경하고 WPF에 속성 변경 사실을 알립니다.
        set => SetProperty(ref _title, value);
    }
}