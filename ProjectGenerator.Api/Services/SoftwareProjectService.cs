using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using ProjectGenerator.Domain.Models;

namespace ProjectGenerator.Api.Services;

/// <summary>
/// Сервис программных проектов, объединяющий кэширование (Redis) и публикацию
/// сгенерированных проектов в брокер сообщений (SQS) для файлового сервиса
/// </summary>
/// <param name="generator">Генератор программных проектов на основе Bogus</param>
/// <param name="producer">Продюсер сообщений, отправляющий проект в очередь</param>
/// <param name="cache">Распределённый кэш Redis для хранения уже сгенерированных проектов</param>
/// <param name="configuration">Конфигурация приложения (содержит TTL кэша)</param>
/// <param name="logger">Логгер для диагностики операций сервиса</param>
public class SoftwareProjectService(
    ISoftwareProjectGenerator generator,
    IProjectProducer producer,
    IDistributedCache cache,
    IConfiguration configuration,
    ILogger<SoftwareProjectService> logger) : ISoftwareProjectService
{
    private const string CacheKeyPrefix = "software-project";
    private readonly TimeSpan _cacheTtl = TimeSpan.FromMinutes(configuration.GetValue("CacheTtlMinutes", 15));

    /// <inheritdoc />
    public async Task<SoftwareProject> GetOrGenerate(int id)
    {
        var cacheKey = $"{CacheKeyPrefix}:{id}";

        var cached = await GetFromCache(cacheKey);
        if (cached is not null)
        {
            return cached;
        }

        logger.LogInformation("Cache miss for id {Id}, generating new data", id);

        var project = generator.Generate(id);

        await producer.SendAsync(project);
        await SetToCache(cacheKey, project);

        return project;
    }

    /// <summary>
    /// Получает программный проект из распределённого кэша по ключу
    /// </summary>
    /// <param name="cacheKey">Полный ключ кэша вида software-project:{id}</param>
    /// <returns>Десериализованный программный проект или null, если в кэше нет данных или произошла ошибка чтения</returns>
    private async Task<SoftwareProject?> GetFromCache(string cacheKey)
    {
        try
        {
            var cached = await cache.GetStringAsync(cacheKey);
            if (cached is null)
            {
                return null;
            }

            logger.LogInformation("Cache hit for key {CacheKey}", cacheKey);
            return JsonSerializer.Deserialize<SoftwareProject>(cached);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to read from cache for key {CacheKey}", cacheKey);
            return null;
        }
    }

    /// <summary>
    /// Сохраняет программный проект в распределённый кэш с настроенным TTL
    /// </summary>
    /// <param name="cacheKey">Полный ключ кэша вида software-project:{id}</param>
    /// <param name="project">Программный проект для сериализации и сохранения</param>
    private async Task SetToCache(string cacheKey, SoftwareProject project)
    {
        try
        {
            var json = JsonSerializer.Serialize(project);
            await cache.SetStringAsync(cacheKey, json, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = _cacheTtl
            });

            logger.LogInformation("Cached data for key {CacheKey}", cacheKey);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to write to cache for key {CacheKey}", cacheKey);
        }
    }
}
