using System.Windows.Controls;

namespace DesktopSchedule.Views;

/// <summary>
/// 월간, 주간, 일간 화면에서 공통으로 사용하는 일정 생성 및 수정 편집 View입니다.
/// 실제 편집 상태와 CRUD 로직은 ScheduleEditorViewModelBase를 상속한 각 화면 ViewModel이 제공합니다.
/// </summary>
public partial class ScheduleEditorView : UserControl
{
    public ScheduleEditorView()
    {
        InitializeComponent();
    }
}