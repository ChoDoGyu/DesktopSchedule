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

        // 새로 설치한 환경에서는 처음부터 최신 구조의 Schedules 테이블을 생성합니다.
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
                IsCompleted INTEGER NOT NULL DEFAULT 0,
                CompletedAt TEXT NULL,
                IsReminderEnabled INTEGER NOT NULL DEFAULT 0,
                ReminderMinutesBefore INTEGER NOT NULL DEFAULT 0,
                CreatedAt TEXT NOT NULL,
                UpdatedAt TEXT NOT NULL
            );
            """;

        command.ExecuteNonQuery();

        // 이전 버전에서 생성한 DB에는 새 컬럼이 없을 수 있으므로
        // 존재하지 않는 컬럼만 추가하여 기존 데이터를 유지합니다.
        AddColumnIfMissing(
            connection,
            "Schedules",
            "IsCompleted",
            "INTEGER NOT NULL DEFAULT 0");

        AddColumnIfMissing(
            connection,
            "Schedules",
            "CompletedAt",
            "TEXT NULL");

        AddColumnIfMissing(
            connection,
            "Schedules",
            "IsReminderEnabled",
            "INTEGER NOT NULL DEFAULT 0");

        AddColumnIfMissing(
            connection,
            "Schedules",
            "ReminderMinutesBefore",
            "INTEGER NOT NULL DEFAULT 0");
    }

    /// <summary>
    /// 지정한 컬럼이 존재하지 않는 경우 기존 테이블에 새 컬럼을 추가합니다.
    /// </summary>
    private static void AddColumnIfMissing(
        SqliteConnection connection,
        string tableName,
        string columnName,
        string columnDefinition)
    {
        if (ColumnExists(connection, tableName, columnName))
        {
            return;
        }

        using var command = connection.CreateCommand();

        // tableName, columnName, columnDefinition은 애플리케이션 내부에서
        // 고정된 값만 전달하며 사용자 입력값을 사용하지 않습니다.
        command.CommandText =
            $"ALTER TABLE {tableName} ADD COLUMN {columnName} {columnDefinition};";

        command.ExecuteNonQuery();
    }

    /// <summary>
    /// 지정한 테이블에 특정 컬럼이 존재하는지 확인합니다.
    /// </summary>
    private static bool ColumnExists(
        SqliteConnection connection,
        string tableName,
        string columnName)
    {
        using var command = connection.CreateCommand();

        // PRAGMA table_info는 테이블에 정의된 컬럼 정보를 반환합니다.
        command.CommandText = $"PRAGMA table_info({tableName});";

        using var reader = command.ExecuteReader();

        while (reader.Read())
        {
            // PRAGMA table_info 결과의 두 번째 값은 컬럼 이름입니다.
            var existingColumnName = reader.GetString(1);

            if (string.Equals(
                existingColumnName,
                columnName,
                StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}