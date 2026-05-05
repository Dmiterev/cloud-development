using System.Text.Json.Serialization;

namespace ProjectGenerator.Domain.Models;

/// <summary>
/// Программный проект — модель предметной области, передаваемая между сервисами генерации,
/// брокером сообщений и объектным хранилищем
/// </summary>
public sealed class SoftwareProject
{
    /// <summary>
    /// Идентификатор в системе
    /// </summary>
    [JsonPropertyName("id")]
    public int Id { get; set; }

    /// <summary>
    /// Название проекта
    /// </summary>
    [JsonPropertyName("projectName")]
    public required string ProjectName { get; init; }

    /// <summary>
    /// Заказчик проекта
    /// </summary>
    [JsonPropertyName("customer")]
    public required string Customer { get; init; }

    /// <summary>
    /// Менеджер проекта
    /// </summary>
    [JsonPropertyName("projectManager")]
    public required string ProjectManager { get; init; }

    /// <summary>
    /// Дата начала
    /// </summary>
    [JsonPropertyName("startDate")]
    public DateOnly StartDate { get; init; }

    /// <summary>
    /// Плановая дата завершения
    /// </summary>
    [JsonPropertyName("plannedEndDate")]
    public DateOnly PlannedEndDate { get; init; }

    /// <summary>
    /// Фактическая дата завершения
    /// </summary>
    [JsonPropertyName("actualEndDate")]
    public DateOnly? ActualEndDate { get; init; }

    /// <summary>
    /// Бюджет
    /// </summary>
    [JsonPropertyName("budget")]
    public decimal Budget { get; init; }

    /// <summary>
    /// Фактические затраты
    /// </summary>
    [JsonPropertyName("actualCosts")]
    public decimal ActualCosts { get; init; }

    /// <summary>
    /// Процент выполнения
    /// </summary>
    [JsonPropertyName("completionPercentage")]
    public int CompletionPercentage { get; init; }
}
