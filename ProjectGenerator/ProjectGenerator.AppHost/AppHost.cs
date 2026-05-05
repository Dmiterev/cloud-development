using Amazon;
using Aspire.Hosting.LocalStack.Container;

var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("cache")
    .WithRedisInsight();

var awsConfig = builder.AddAWSSDKConfig()
    .WithProfile("default")
    .WithRegion(RegionEndpoint.EUCentral1);

var localstack = builder.AddLocalStack(
    "projectgenerator-localstack",
    awsConfig: awsConfig,
    configureContainer: container =>
    {
        container.Lifetime = ContainerLifetime.Session;
        container.DebugLevel = 1;
        container.LogLevel = LocalStackLogLevel.Debug;
        container.Port = 4566;
        container.AdditionalEnvironmentVariables.Add("DEBUG", "1");
    });

var awsResources = builder
    .AddAWSCloudFormationTemplate("resources", "CloudFormation/projectgenerator-template.yaml", "projectgenerator")
    .WithReference(awsConfig);

var apiGateway = builder.AddProject<Projects.Api_Gateway>("api-gateway");

for (var i = 0; i < 5; i++)
{
    var generationApi = builder.AddProject<Projects.ProjectGenerator_Api>($"generation-api-{i}", launchProfileName: null)
        .WithHttpsEndpoint(5000 + i)
        .WithReference(cache)
        .WithReference(awsResources)
        .WaitFor(cache)
        .WaitFor(awsResources);
    apiGateway.WaitFor(generationApi);
}

builder.AddProject<Projects.Client_Wasm>("client-wasm")
    .WaitFor(apiGateway);

builder.AddProject<Projects.ProjectGenerator_FileService>("projectgenerator-fileservice")
    .WithReference(awsResources)
    .WaitFor(awsResources);

builder.UseLocalStack(localstack);

builder.Build().Run();
