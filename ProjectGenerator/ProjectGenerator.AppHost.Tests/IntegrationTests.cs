using System.Text.Json;
using Aspire.Hosting;
using Microsoft.Extensions.Logging;
using ProjectGenerator.Domain.Models;
using Xunit.Abstractions;

namespace ProjectGenerator.AppHost.Tests;

/// <summary>
/// Интеграционные тесты приложения, поднимаемого через AppHost
/// </summary>
public class IntegrationTests(ITestOutputHelper output) : IAsyncLifetime
{
    private DistributedApplication? _app;
    private HttpClient? _gatewayClient;
    private HttpClient? _sinkClient;

    public async Task InitializeAsync()
    {
        var cancellationToken = CancellationToken.None;
        var builder = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.ProjectGenerator_AppHost>(cancellationToken);

        builder.Configuration["DcpPublisher:RandomizePorts"] = "false";
        builder.Services.AddLogging(logging =>
        {
            logging.AddXUnit(output);
            logging.SetMinimumLevel(LogLevel.Debug);
            logging.AddFilter("Aspire.Hosting.Dcp", LogLevel.Debug);
            logging.AddFilter("Aspire.Hosting", LogLevel.Debug);
        });

        _app = await builder.BuildAsync(cancellationToken);
        await _app.StartAsync(cancellationToken);
        _gatewayClient = _app!.CreateHttpClient("api-gateway", "http");
        _sinkClient = _app!.CreateHttpClient("projectgenerator-fileservice", "http");
    }

    /// <summary>
    /// Проверяет, что объект, отданный API, попадает в S3 в том же виде
    /// </summary>
    [Fact]
    public async Task Pipeline_Generates_PutsToS3_AndContentMatches()
    {
        var id = Random.Shared.Next(1, 100);

        using var gatewayResponse = await _gatewayClient!.GetAsync($"/generate?id={id}");
        Assert.Equal(HttpStatusCode.OK, gatewayResponse.StatusCode);
        var apiProject = JsonSerializer.Deserialize<SoftwareProject>(
            await gatewayResponse.Content.ReadAsStringAsync());

        await Task.Delay(5000);

        using var s3Response = await _sinkClient!.GetAsync($"/api/s3/software-project_{id}.json");
        Assert.Equal(HttpStatusCode.OK, s3Response.StatusCode);
        var s3Project = JsonSerializer.Deserialize<SoftwareProject>(
            await s3Response.Content.ReadAsStringAsync());

        Assert.NotNull(apiProject);
        Assert.NotNull(s3Project);
        Assert.Equal(id, s3Project!.Id);
        Assert.Equivalent(apiProject, s3Project);
    }

    /// <summary>
    /// Два запроса с одним id должны вернуть одинаковый объект
    /// </summary>
    [Fact]
    public async Task Cache_ReturnsIdenticalProject_OnRepeatedRequest()
    {
        var id = Random.Shared.Next(1, 100);

        using var first = await _gatewayClient!.GetAsync($"/generate?id={id}");
        using var second = await _gatewayClient!.GetAsync($"/generate?id={id}");

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);

        var firstProject = JsonSerializer.Deserialize<SoftwareProject>(
            await first.Content.ReadAsStringAsync());
        var secondProject = JsonSerializer.Deserialize<SoftwareProject>(
            await second.Content.ReadAsStringAsync());

        Assert.NotNull(firstProject);
        Assert.NotNull(secondProject);
        Assert.Equivalent(firstProject, secondProject);
    }

    public async Task DisposeAsync()
    {
        if (_app is not null)
        {
            await _app.StopAsync();
            await _app.DisposeAsync();
        }
    }
}
