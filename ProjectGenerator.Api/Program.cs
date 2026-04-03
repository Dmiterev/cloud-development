using ProjectGenerator.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddRedisDistributedCache("cache");

builder.Services.AddSingleton<ISoftwareProjectGenerator, SoftwareProjectGenerator>();
builder.Services.AddScoped<ISoftwareProjectService, SoftwareProjectService>();

var app = builder.Build();

app.MapDefaultEndpoints();

app.MapGet("/api/generate", async (int id, ISoftwareProjectService service) =>
    await service.GetOrGenerate(id));

app.Run();
