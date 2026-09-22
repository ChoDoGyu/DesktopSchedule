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
    /// 파일이 없거나 접근할 수 없으면 null을 반환합니다.
    /// JSON이 손상된 경우에는 손상 파일을 안전하게 제거한 뒤 null을 반환합니다.
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
            var value = JsonSerializer.Deserialize<T>(json, SerializerOptions);

            if (value is null)
            {
                TryDeleteCorruptedFile();
            }

            return value;
        }
        catch (JsonException)
        {
            TryDeleteCorruptedFile();
            return null;
        }
        catch (NotSupportedException)
        {
            TryDeleteCorruptedFile();
            return null;
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
    }

    /// <summary>
    /// 전달받은 데이터를 임시 JSON 파일에 먼저 기록한 뒤 실제 저장 파일로 교체합니다.
    /// 저장 도중 애플리케이션이 종료되어 기존 파일이 불완전하게 기록되는 위험을 줄입니다.
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

        var temporaryFilePath = $"{_filePath}.tmp";
        var json = JsonSerializer.Serialize(value, SerializerOptions);

        try
        {
            File.WriteAllText(temporaryFilePath, json);
            File.Move(temporaryFilePath, _filePath, true);
        }
        finally
        {
            TryDeleteTemporaryFile(temporaryFilePath);
        }
    }

    /// <summary>
    /// 현재 저장된 JSON 파일을 삭제합니다.
    /// 파일이 존재하지 않는 경우에는 아무 작업도 하지 않습니다.
    /// </summary>
    public void Delete()
    {
        File.Delete(_filePath);
    }

    /// <summary>
    /// 손상된 JSON 파일 제거를 시도합니다.
    /// 제거에 실패하더라도 설정 파일 손상 때문에 애플리케이션 시작이 중단되지 않도록 합니다.
    /// </summary>
    private void TryDeleteCorruptedFile()
    {
        try
        {
            File.Delete(_filePath);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    /// <summary>
    /// 저장 과정에서 남을 수 있는 임시 파일 제거를 시도합니다.
    /// 원래 저장 예외가 임시 파일 정리 실패로 가려지지 않도록 정리 실패는 전파하지 않습니다.
    /// </summary>
    private static void TryDeleteTemporaryFile(string temporaryFilePath)
    {
        try
        {
            File.Delete(temporaryFilePath);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}