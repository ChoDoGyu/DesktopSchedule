using System.IO;
using System.Text.Json;

namespace DesktopSchedule.Services;

/// <summary>
/// DesktopSchedule의 사용자별 LocalAppData 영역에
/// 특정 형식의 데이터를 JSON 파일로 저장하고 불러오는 공통 저장소입니다.
/// </summary>
/// <typeparam name="T">JSON으로 저장할 참조 형식입니다.</typeparam>
public sealed class LocalJsonStore<T> where T : class
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true
    };

    private readonly string _filePath;

    public LocalJsonStore(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException("저장 파일 이름은 비어 있을 수 없습니다.", nameof(fileName));
        }

        var localAppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var dataDirectoryPath = Path.Combine(localAppDataPath, "DesktopSchedule");

        _filePath = Path.Combine(dataDirectoryPath, fileName);
    }

    /// <summary>
    /// 저장된 JSON 파일을 읽어 지정한 형식으로 역직렬화합니다.
    /// 파일이 없거나 손상되었거나 접근할 수 없으면 null을 반환합니다.
    /// </summary>
    public T? Load()
    {
        if (!File.Exists(_filePath))
        {
            return null;
        }

        try
        {
            var json = File.ReadAllText(_filePath);
            return JsonSerializer.Deserialize<T>(json, SerializerOptions);
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// 전달받은 데이터를 JSON으로 직렬화하여 사용자별 LocalAppData 영역에 저장합니다.
    /// </summary>
    public void Save(T value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var directoryPath = Path.GetDirectoryName(_filePath);

        if (string.IsNullOrWhiteSpace(directoryPath))
        {
            throw new InvalidOperationException("로컬 JSON 저장 경로를 확인할 수 없습니다.");
        }

        Directory.CreateDirectory(directoryPath);

        var json = JsonSerializer.Serialize(value, SerializerOptions);
        File.WriteAllText(_filePath, json);
    }

    /// <summary>
    /// 현재 저장된 JSON 파일을 삭제합니다.
    /// 파일이 존재하지 않는 경우에는 아무 작업도 하지 않습니다.
    /// </summary>
    public void Delete()
    {
        File.Delete(_filePath);
    }
}