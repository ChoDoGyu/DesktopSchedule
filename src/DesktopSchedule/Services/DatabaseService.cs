using System.IO; // Path, Directory를 사용하기 위해 필요합니다.
using Microsoft.Data.Sqlite; // SQLite 데이터베이스 연결을 사용하기 위해 필요합니다.

namespace DesktopSchedule.Services;

/// <summary>
/// 애플리케이션에서 사용할 SQLite 데이터베이스의 경로와 초기화를 관리합니다.
/// </summary>
public class DatabaseService
{
    // 애플리케이션의 로컬 데이터가 저장될 폴더 경로입니다.
    private readonly string _dataDirectoryPath;

    /// <summary>
    /// SQLite 데이터베이스 파일의 전체 경로입니다.
    /// </summary>
    public string DatabasePath { get; }

    public DatabaseService()
    {
        // Windows의 사용자별 LocalAppData 폴더 경로를 가져옵니다.
        var localAppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        // DesktopSchedule 전용 데이터 폴더 경로를 구성합니다.
        _dataDirectoryPath = Path.Combine(localAppDataPath, "DesktopSchedule");

        // 전용 데이터 폴더 안에 SQLite 데이터베이스 파일 경로를 구성합니다.
        DatabasePath = Path.Combine(_dataDirectoryPath, "desktop-schedule.db");
    }

    /// <summary>
    /// 데이터 저장 폴더와 SQLite 데이터베이스 테이블을 준비합니다.
    /// </summary>
    public void Initialize()
    {
        // 데이터 폴더가 없다면 생성합니다.
        Directory.CreateDirectory(_dataDirectoryPath);

        // SQLite 데이터베이스에 연결합니다.
        // 파일이 아직 없다면 SQLite가 연결 과정에서 새 파일을 생성합니다.
        using var connection = new SqliteConnection($"Data Source={DatabasePath}");
        connection.Open();

        // 일정 데이터를 저장할 Schedules 테이블을 생성하는 SQL 명령을 준비합니다.
        // IF NOT EXISTS를 사용하므로 이미 테이블이 존재해도 오류가 발생하지 않습니다.
        using var command = connection.CreateCommand();

        command.CommandText =
            """
            CREATE TABLE IF NOT EXISTS Schedules
            (
                Id TEXT PRIMARY KEY NOT NULL,
                Title TEXT NOT NULL,
                Description TEXT NOT NULL,
                StartAt TEXT NOT NULL,
                EndAt TEXT NOT NULL,
                IsAllDay INTEGER NOT NULL,
                CreatedAt TEXT NOT NULL,
                UpdatedAt TEXT NOT NULL
            );
            """;

        // 테이블 생성 명령을 실행합니다.
        command.ExecuteNonQuery();
    }
}