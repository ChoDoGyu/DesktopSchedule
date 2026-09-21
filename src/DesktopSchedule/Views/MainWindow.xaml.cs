using System.Windows; // Window 클래스를 사용하기 위해 필요합니다.
using DesktopSchedule.ViewModels; // MainWindowViewModel을 사용하기 위해 필요합니다.

namespace DesktopSchedule.Views;

/// <summary>
/// 애플리케이션의 메인 창입니다.
/// </summary>
public partial class MainWindow : Window
{
    /// <summary>
    /// MainWindow에서 사용할 ViewModel을 전달받아 창을 생성합니다.
    /// </summary>
    public MainWindow(MainWindowViewModel viewModel)
    {
        // XAML에 정의된 MainWindow UI를 초기화합니다.
        InitializeComponent();

        // 외부에서 생성된 ViewModel을 MainWindow의 DataContext로 사용합니다.
        // MainWindow가 직접 ViewModel을 생성하지 않도록 책임을 분리합니다.
        DataContext = viewModel;
    }
}