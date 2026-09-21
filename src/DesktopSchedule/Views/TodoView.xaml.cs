using System.Windows.Controls; // UserControl 클래스를 사용하기 위해 필요합니다.

namespace DesktopSchedule.Views;

/// <summary>
/// 할 일 목록을 표시하는 View입니다.
/// </summary>
public partial class TodoView : UserControl
{
    public TodoView()
    {
        // XAML에 정의된 UI 요소를 생성하고 연결합니다.
        InitializeComponent();
    }
}