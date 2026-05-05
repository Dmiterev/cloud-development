using System.Net;
using System.Text.Json;
using Amazon.SQS;
using ProjectGenerator.Domain.Models;

namespace ProjectGenerator.Api.Services;

/// <summary>
/// Реализация продюсера на основе AWS SQS — сериализует программный проект в JSON
/// и отправляет в очередь, имя которой берётся из конфигурации CloudFormation-стэка
/// </summary>
/// <param name="client">SQS-клиент, сконфигурированный через LocalStack-расширения</param>
/// <param name="configuration">Конфигурация приложения (содержит имя очереди, пробрасываемое из AppHost)</param>
/// <param name="logger">Логгер для диагностики операций отправки</param>
public class SqsProjectProducer(
    IAmazonSQS client,
    IConfiguration configuration,
    ILogger<SqsProjectProducer> logger) : IProjectProducer
{
    private readonly string _queueName = configuration["AWS:Resources:SQSQueueName"]
        ?? throw new KeyNotFoundException("SQS queue name was not found in configuration");

    /// <inheritdoc/>
    public async Task SendAsync(SoftwareProject project)
    {
        try
        {
            var json = JsonSerializer.Serialize(project);
            var response = await client.SendMessageAsync(_queueName, json);

            if (response.HttpStatusCode == HttpStatusCode.OK)
            {
                logger.LogInformation("Software project {Id} was sent to SQS queue {Queue}", project.Id, _queueName);
            }
            else
            {
                throw new InvalidOperationException($"SQS returned {response.HttpStatusCode}");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unable to send software project {Id} through SQS queue {Queue}", project.Id, _queueName);
        }
    }
}
