using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc;
using ProjectGenerator.FileService.Storage;

namespace ProjectGenerator.FileService.Controllers;

/// <summary>
/// REST-контроллер для просмотра содержимого объектного хранилища:
/// получение списка ключей и скачивание отдельного файла по ключу
/// </summary>
/// <param name="s3Service">Служба для работы с S3</param>
/// <param name="logger">Логгер для трассировки вызовов контроллера</param>
[ApiController]
[Route("api/s3")]
public class S3StorageController(
    IS3Service s3Service,
    ILogger<S3StorageController> logger) : ControllerBase
{
    /// <summary>
    /// Возвращает список ключей всех объектов, сохранённых в бакете
    /// </summary>
    /// <returns>200 со списком ключей, 500 при ошибке доступа к хранилищу</returns>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<List<string>>> ListFiles()
    {
        try
        {
            var list = await s3Service.GetFileListAsync();
            logger.LogInformation("Returning {Count} keys from bucket", list.Count);
            return Ok(list);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to list files");
            return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
        }
    }

    /// <summary>
    /// Возвращает содержимое JSON-файла из бакета по указанному ключу
    /// </summary>
    /// <param name="key">Ключ объекта в бакете (например, software-project_42.json)</param>
    /// <returns>200 с JSON-содержимым файла, 500 при ошибке скачивания</returns>
    [HttpGet("{key}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<JsonNode>> GetFile(string key)
    {
        try
        {
            var node = await s3Service.DownloadFileAsync(key);
            return Ok(node);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to download {Key}", key);
            return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
        }
    }
}
