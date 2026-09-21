using System.Windows.Controls; // UserControl 클래스를 사용하기 위해 필요합니다.

namespace DesktopSchedule.Views;

/// <summary>
/// 월간 달력 화면을 표시하는 View입니다.
/// </summary>
public partial class MonthlyCalendarView : UserControl
{
    public MonthlyCalendarView()
    {
        // XAML에 정의된 UI 요소를 생성하고 연결합니다.
        InitializeComponent();
    }
}