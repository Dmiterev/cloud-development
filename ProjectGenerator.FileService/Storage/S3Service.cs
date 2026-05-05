using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Amazon.S3;
using Amazon.S3.Model;

namespace ProjectGenerator.FileService.Storage;

/// <summary>
/// Реализация службы объектного хранилища поверх AWS S3 SDK,
/// сконфигурированного через LocalStack-расширения
/// </summary>
/// <param name="client">Клиент S3 (через LocalStack)</param>
/// <param name="configuration">Конфигурация приложения (содержит имя бакета из CloudFormation-стэка)</param>
/// <param name="logger">Логгер для диагностики операций со хранилищем</param>
public class S3Service(
    IAmazonS3 client,
    IConfiguration configuration,
    ILogger<S3Service> logger) : IS3Service
{
    private readonly string _bucketName = configuration["AWS:Resources:S3BucketName"]
        ?? throw new KeyNotFoundException("S3 bucket name was not found in configuration");

    /// <inheritdoc/>
    public async Task<List<string>> GetFileListAsync()
    {
        var list = new List<string>();
        var request = new ListObjectsV2Request
        {
            BucketName = _bucketName,
            Prefix = "",
            Delimiter = ","
        };
        var paginator = client.Paginators.ListObjectsV2(request);

        logger.LogInformation("Listing files in bucket {Bucket}", _bucketName);
        await foreach (var response in paginator.Responses)
        {
            if (response?.S3Objects is null)
            {
                logger.LogWarning("Received null response from {Bucket}", _bucketName);
                continue;
            }

            foreach (var obj in response.S3Objects)
            {
                if (obj is not null)
                {
                    list.Add(obj.Key);
                }
            }
        }

        return list;
    }

    /// <inheritdoc/>
    public async Task<bool> UploadFileAsync(string fileData)
    {
        var rootNode = JsonNode.Parse(fileData)
            ?? throw new ArgumentException("Passed string is not a valid JSON");
        var id = rootNode["id"]?.GetValue<int>()
            ?? throw new ArgumentException("Passed JSON does not contain integer id");

        using var stream = new MemoryStream();
        JsonSerializer.Serialize(stream, rootNode);
        stream.Seek(0, SeekOrigin.Begin);

        var key = $"software-project_{id}.json";
        logger.LogInformation("Uploading software project {Id} as {Key} to {Bucket}", id, key, _bucketName);

        var request = new PutObjectRequest
        {
            BucketName = _bucketName,
            Key = key,
            InputStream = stream
        };

        var response = await client.PutObjectAsync(request);

        if (response.HttpStatusCode != HttpStatusCode.OK)
        {
            logger.LogError("Failed to upload software project {Id}: {Code}", id, response.HttpStatusCode);
            return false;
        }

        logger.LogInformation("Software project {Id} uploaded to {Bucket}", id, _bucketName);
        return true;
    }

    /// <inheritdoc/>
    public async Task<JsonNode> DownloadFileAsync(string key)
    {
        logger.LogInformation("Downloading {Key} from {Bucket}", key, _bucketName);

        try
        {
            var request = new GetObjectRequest
            {
                BucketName = _bucketName,
                Key = key
            };
            using var response = await client.GetObjectAsync(request);

            if (response.HttpStatusCode != HttpStatusCode.OK)
            {
                logger.LogError("Failed to download {Key}: {Code}", key, response.HttpStatusCode);
                throw new InvalidOperationException($"Error occurred downloading {key} - {response.HttpStatusCode}");
            }

            using var reader = new StreamReader(response.ResponseStream, Encoding.UTF8);
            var content = await reader.ReadToEndAsync();
            return JsonNode.Parse(content)
                ?? throw new InvalidOperationException("Downloaded document is not a valid JSON");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Exception during {Key} download", key);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task EnsureBucketExistsAsync()
    {
        logger.LogInformation("Checking whether bucket {Bucket} exists", _bucketName);
        try
        {
            await client.EnsureBucketExistsAsync(_bucketName);
            logger.LogInformation("Bucket {Bucket} existence ensured", _bucketName);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception during {Bucket} existence check", _bucketName);
            throw;
        }
    }
}
