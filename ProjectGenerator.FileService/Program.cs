using System.Reflection;
using Amazon.S3;
using Amazon.SQS;
using LocalStack.Client.Extensions;
using ProjectGenerator.FileService.Messaging;
using ProjectGenerator.FileService.Storage;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    var assembly = Assembly.GetExecutingAssembly();
    var xmlPath = Path.Combine(AppContext.BaseDirectory, $"{assembly.GetName().Name}.xml");
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

builder.Services.AddLocalStack(builder.Configuration);
builder.Services.AddAwsService<IAmazonSQS>();
builder.Services.AddAwsService<IAmazonS3>();
builder.Services.AddHostedService<SqsConsumerService>();
builder.Services.AddScoped<IS3Service, S3Service>();

var app = builder.Build();

app.MapDefaultEndpoints();

using var scope = app.Services.CreateScope();
var s3Service = scope.ServiceProvider.GetRequiredService<IS3Service>();
await s3Service.EnsureBucketExistsAsync();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

app.Run();
