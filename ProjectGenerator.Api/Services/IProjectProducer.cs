using ProjectGenerator.Domain.Models;

namespace ProjectGenerator.Api.Services;

/// <summary>
/// Интерфейс продюсера, отправляющего сгенерированные программные проекты
/// в брокер сообщений (SQS) для последующей обработки файловым сервисом
/// </summary>
public interface IProjectProducer
{
    /// <summary>
    /// Сериализует и отправляет программный проект в очередь сообщений
    /// </summary>
    /// <param name="project">Сгенерированный программный проект для отправки</param>
    /// <returns>Задача, завершающаяся после отправки сообщения в очередь</returns>
    public Task SendAsync(SoftwareProject project);
}
