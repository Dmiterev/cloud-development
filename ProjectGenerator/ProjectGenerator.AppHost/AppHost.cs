var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("cache")
    .WithRedisInsight();

var apiGateway = builder.AddProject<Projects.Api_Gateway>("api-gateway");

for (var i = 0; i < 5; i++)
{
    var generationApi = builder.AddProject<Projects.ProjectGenerator_Api>($"generation-api-{i}", launchProfileName: null)
        .WithHttpsEndpoint(5000 + i)
        .WithReference(cache)
        .WaitFor(cache);
    apiGateway.WaitFor(generationApi);
}

builder.AddProject<Projects.Client_Wasm>("client-wasm")
    .WaitFor(apiGateway);

builder.Build().Run();
