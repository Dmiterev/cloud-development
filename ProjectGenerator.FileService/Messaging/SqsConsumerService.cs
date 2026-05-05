using Amazon.SQS;
using Amazon.SQS.Model;
using ProjectGenerator.FileService.Storage;

namespace ProjectGenerator.FileService.Messaging;

/// <summary>
/// Фоновая служба, читающая сообщения из SQS-очереди батчами
/// и передающая каждое сообщение в S3-сервис для сохранения файла в бакете
/// </summary>
/// <param name="sqsClient">Клиент SQS (через LocalStack)</param>
/// <param name="scopeFactory">Фабрика скоупов DI для получения scoped IS3Service на каждое сообщение</param>
/// <param name="configuration">Конфигурация приложения (содержит имя очереди из CloudFormation-стэка)</param>
/// <param name="logger">Логгер для диагностики приёма и обработки сообщений</param>
public class SqsConsumerService(
    IAmazonSQS sqsClient,
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<SqsConsumerService> logger) : BackgroundService
{
    private const int MaxMessagesPerBatch = 10;
    private const int LongPollSeconds = 5;

    private readonly string _queueName = configuration["AWS:Resources:SQSQueueName"]
        ?? throw new KeyNotFoundException("SQS queue name was not found in configuration");

    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("SQS consumer service started for queue {Queue}", _queueName);

        while (!stoppingToken.IsCancellationRequested)
        {
            var response = await sqsClient.ReceiveMessageAsync(
                new ReceiveMessageRequest
                {
                    QueueUrl = _queueName,
                    MaxNumberOfMessages = MaxMessagesPerBatch,
                    WaitTimeSeconds = LongPollSeconds
                }, stoppingToken);

            if (response?.Messages is null || response.Messages.Count == 0)
            {
                continue;
            }

            logger.LogInformation("Received {Count} messages from {Queue}", response.Messages.Count, _queueName);

            foreach (var message in response.Messages)
            {
                try
                {
                    using var scope = scopeFactory.CreateScope();
                    var s3Service = scope.ServiceProvider.GetRequiredService<IS3Service>();
                    await s3Service.UploadFileAsync(message.Body);

                    await sqsClient.DeleteMessageAsync(_queueName, message.ReceiptHandle, stoppingToken);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error processing message {MessageId}", message.MessageId);
                }
            }
        }
    }
}
