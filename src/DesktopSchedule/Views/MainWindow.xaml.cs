using System.Windows; // Window 클래스를 사용하기 위해 필요합니다.
using DesktopSchedule.ViewModels; // MainWindowViewModel을 사용하기 위해 필요합니다.

namespace DesktopSchedule.Views; // View 관련 클래스가 속하는 네임스페이스입니다.

public partial class MainWindow : Window
{
    public MainWindow()
    {
        // XAML에 정의된 MainWindow UI를 초기화합니다.
        InitializeComponent();

        // MainWindow에서 사용할 ViewModel을 생성하고 DataContext로 지정합니다.
        // 이후 XAML의 Binding은 이 ViewModel의 프로퍼티를 기준으로 찾게 됩니다.
        DataContext = new MainWindowViewModel();
    }
}