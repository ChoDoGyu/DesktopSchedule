using System.Windows.Controls; // UserControl 클래스를 사용하기 위해 필요합니다.

namespace DesktopSchedule.Views;

/// <summary>
/// 주간 일정 화면을 표시하는 View입니다.
/// </summary>
public partial class WeeklyScheduleView : UserControl
{
    public WeeklyScheduleView()
    {
        // XAML에 정의된 UI 요소를 생성하고 연결합니다.
        InitializeComponent();
    }
}