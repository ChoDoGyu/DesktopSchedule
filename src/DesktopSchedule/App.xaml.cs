using System.Windows; // Application, StartupEventArgs를 사용하기 위해 필요합니다.
using DesktopSchedule.Services; // DatabaseService를 사용하기 위해 필요합니다.
using DesktopSchedule.ViewModels; // MainWindowViewModel을 생성하기 위해 필요합니다.
using DesktopSchedule.Views; // MainWindow를 생성하기 위해 필요합니다.

namespace DesktopSchedule;

/// <summary>
/// 애플리케이션의 시작과 종료 흐름을 관리합니다.
/// </summary>
public partial class App : Application
{
    /// <summary>
    /// 애플리케이션이 시작될 때 필요한 객체를 생성하고 MainWindow를 표시합니다.
    /// </summary>
    protected override void OnStartup(StartupEventArgs e)
    {
        // Application의 기본 시작 처리를 먼저 수행합니다.
        base.OnStartup(e);

        // 애플리케이션에서 사용할 SQLite 데이터베이스 서비스를 생성합니다.
        var databaseService = new DatabaseService();

        // 로컬 데이터 폴더와 필요한 데이터베이스 테이블을 준비합니다.
        databaseService.Initialize();

        // MainWindow에서 사용할 ViewModel을 생성합니다.
        var mainWindowViewModel = new MainWindowViewModel();

        // 생성된 ViewModel을 MainWindow에 전달합니다.
        var mainWindow = new MainWindow(mainWindowViewModel);

        // MainWindow를 화면에 표시합니다.
        mainWindow.Show();
    }
}