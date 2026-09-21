using System.Globalization; // DateTime을 일정한 형식으로 저장하고 복원하기 위해 필요합니다.
using DesktopSchedule.Models; // ScheduleItem을 사용하기 위해 필요합니다.
using DesktopSchedule.Services; // DatabaseService를 사용하기 위해 필요합니다.
using Microsoft.Data.Sqlite; // SQLite 연결과 명령을 사용하기 위해 필요합니다.

namespace DesktopSchedule.Repositories;

/// <summary>
/// SQLite를 사용하여 일정 데이터를 저장하고 불러옵니다.
/// </summary>
public class SqliteScheduleRepository : IScheduleRepository
{
    // SQLite 데이터베이스 연결 문자열입니다.
    private readonly string _connectionString;

    public SqliteScheduleRepository(DatabaseService databaseService)
    {
        // DatabaseService가 관리하는 DB 파일 경로를 이용해 연결 문자열을 구성합니다.
        _connectionString = $"Data Source={databaseService.DatabasePath}";
    }

    /// <summary>
    /// 저장된 모든 일정을 시작 시간 순으로 반환합니다.
    /// </summary>
    public IReadOnlyList<ScheduleItem> GetAll()
    {
        var schedules = new List<ScheduleItem>();

        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var command = connection.CreateCommand();

        command.CommandText =
            """
            SELECT
                Id,
                Title,
                Description,
                StartAt,
                EndAt,
                IsAllDay,
                IsCompleted,
                CompletedAt,
                IsReminderEnabled,
                ReminderMinutesBefore,
                CreatedAt,
                UpdatedAt
            FROM Schedules
            ORDER BY StartAt;
            """;

        using var reader = command.ExecuteReader();

        while (reader.Read())
        {
            schedules.Add(ReadSchedule(reader));
        }

        return schedules;
    }

    /// <summary>
    /// 지정한 Id와 일치하는 일정을 반환합니다.
    /// 일정이 존재하지 않으면 null을 반환합니다.
    /// </summary>
    public ScheduleItem? GetById(Guid id)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var command = connection.CreateCommand();

        command.CommandText =
            """
            SELECT
                Id,
                Title,
                Description,
                StartAt,
                EndAt,
                IsAllDay,
                IsCompleted,
                CompletedAt,
                IsReminderEnabled,
                ReminderMinutesBefore,
                CreatedAt,
                UpdatedAt
            FROM Schedules
            WHERE Id = $id;
            """;

        command.Parameters.AddWithValue("$id", id.ToString());

        using var reader = command.ExecuteReader();

        if (!reader.Read())
        {
            return null;
        }

        return ReadSchedule(reader);
    }

    /// <summary>
    /// 새로운 일정을 SQLite 데이터베이스에 저장합니다.
    /// </summary>
    public void Add(ScheduleItem schedule)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var command = connection.CreateCommand();

        command.CommandText =
            """
            INSERT INTO Schedules
            (
                Id,
                Title,
                Description,
                StartAt,
                EndAt,
                IsAllDay,
                IsCompleted,
                CompletedAt,
                IsReminderEnabled,
                ReminderMinutesBefore,
                CreatedAt,
                UpdatedAt
            )
            VALUES
            (
                $id,
                $title,
                $description,
                $startAt,
                $endAt,
                $isAllDay,
                $isCompleted,
                $completedAt,
                $isReminderEnabled,
                $reminderMinutesBefore,
                $createdAt,
                $updatedAt
            );
            """;

        AddScheduleParameters(command, schedule);

        command.ExecuteNonQuery();
    }

    /// <summary>
    /// 기존 일정 정보를 SQLite 데이터베이스에서 수정합니다.
    /// </summary>
    public void Update(ScheduleItem schedule)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var command = connection.CreateCommand();

        command.CommandText =
            """
            UPDATE Schedules
            SET
                Title = $title,
                Description = $description,
                StartAt = $startAt,
                EndAt = $endAt,
                IsAllDay = $isAllDay,
                IsCompleted = $isCompleted,
                CompletedAt = $completedAt,
                IsReminderEnabled = $isReminderEnabled,
                ReminderMinutesBefore = $reminderMinutesBefore,
                CreatedAt = $createdAt,
                UpdatedAt = $updatedAt
            WHERE Id = $id;
            """;

        AddScheduleParameters(command, schedule);

        command.ExecuteNonQuery();
    }

    /// <summary>
    /// 지정한 Id의 일정을 SQLite 데이터베이스에서 삭제합니다.
    /// </summary>
    public void Delete(Guid id)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var command = connection.CreateCommand();

        command.CommandText =
            """
            DELETE FROM Schedules
            WHERE Id = $id;
            """;

        command.Parameters.AddWithValue("$id", id.ToString());

        command.ExecuteNonQuery();
    }

    /// <summary>
    /// ScheduleItem의 값을 SQLite 명령의 파라미터로 추가합니다.
    /// </summary>
    private static void AddScheduleParameters(
        SqliteCommand command,
        ScheduleItem schedule)
    {
        command.Parameters.AddWithValue("$id", schedule.Id.ToString());
        command.Parameters.AddWithValue("$title", schedule.Title);
        command.Parameters.AddWithValue("$description", schedule.Description);

        // "O" 형식은 날짜와 시간을 손실 없이 저장하기 위한 ISO 8601 기반 형식입니다.
        command.Parameters.AddWithValue(
            "$startAt",
            schedule.StartAt.ToString("O", CultureInfo.InvariantCulture));

        command.Parameters.AddWithValue(
            "$endAt",
            schedule.EndAt.ToString("O", CultureInfo.InvariantCulture));

        // SQLite에는 bool 전용 타입이 없으므로 false는 0, true는 1로 저장합니다.
        command.Parameters.AddWithValue(
            "$isAllDay",
            schedule.IsAllDay ? 1 : 0);

        command.Parameters.AddWithValue(
            "$isCompleted",
            schedule.IsCompleted ? 1 : 0);

        // 완료되지 않은 일정의 CompletedAt은 SQLite NULL로 저장합니다.
        command.Parameters.AddWithValue(
            "$completedAt",
            schedule.CompletedAt.HasValue
                ? schedule.CompletedAt.Value.ToString("O", CultureInfo.InvariantCulture)
                : DBNull.Value);

        command.Parameters.AddWithValue(
            "$isReminderEnabled",
            schedule.IsReminderEnabled ? 1 : 0);

        command.Parameters.AddWithValue(
            "$reminderMinutesBefore",
            schedule.ReminderMinutesBefore);

        command.Parameters.AddWithValue(
            "$createdAt",
            schedule.CreatedAt.ToString("O", CultureInfo.InvariantCulture));

        command.Parameters.AddWithValue(
            "$updatedAt",
            schedule.UpdatedAt.ToString("O", CultureInfo.InvariantCulture));
    }

    /// <summary>
    /// 현재 SQLite 조회 결과를 ScheduleItem으로 변환합니다.
    /// </summary>
    private static ScheduleItem ReadSchedule(SqliteDataReader reader)
    {
        return new ScheduleItem
        {
            Id = Guid.Parse(reader.GetString(0)),
            Title = reader.GetString(1),
            Description = reader.GetString(2),

            StartAt = DateTime.Parse(
                reader.GetString(3),
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind),

            EndAt = DateTime.Parse(
                reader.GetString(4),
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind),

            IsAllDay = reader.GetInt32(5) != 0,

            IsCompleted = reader.GetInt32(6) != 0,

            CompletedAt = reader.IsDBNull(7)
                ? null
                : DateTime.Parse(
                    reader.GetString(7),
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind),

            IsReminderEnabled = reader.GetInt32(8) != 0,

            ReminderMinutesBefore = reader.GetInt32(9),

            CreatedAt = DateTime.Parse(
                reader.GetString(10),
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind),

            UpdatedAt = DateTime.Parse(
                reader.GetString(11),
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind)
        };
    }
}