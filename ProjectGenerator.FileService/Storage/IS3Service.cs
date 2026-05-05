using System.Text.Json.Nodes;

namespace ProjectGenerator.FileService.Storage;

/// <summary>
/// Интерфейс службы для манипуляции файлами в объектном хранилище S3
/// </summary>
public interface IS3Service
{
    /// <summary>
    /// Сериализует переданное JSON-представление программного проекта и загружает его
    /// в бакет под ключом software-project_{id}.json
    /// </summary>
    /// <param name="fileData">JSON-строка с данными программного проекта (должна содержать поле id)</param>
    /// <returns>true — если объект успешно загружен; false — если получен не успешный код от S3</returns>
    public Task<bool> UploadFileAsync(string fileData);

    /// <summary>
    /// Возвращает список ключей всех объектов, хранящихся в бакете
    /// </summary>
    /// <returns>Список ключей файлов</returns>
    public Task<List<string>> GetFileListAsync();

    /// <summary>
    /// Скачивает объект из бакета по ключу и парсит его как JSON
    /// </summary>
    /// <param name="key">Ключ объекта в бакете</param>
    /// <returns>Корневой узел JSON документа, считанного из S3</returns>
    public Task<JsonNode> DownloadFileAsync(string key);

    /// <summary>
    /// Создаёт целевой бакет, если он ещё не существует. Вызывается при старте приложения
    /// </summary>
    public Task EnsureBucketExistsAsync();
}
