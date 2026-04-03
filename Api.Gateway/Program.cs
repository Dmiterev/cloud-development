using Api.Gateway.LoadBalancers;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddServiceDiscovery();
builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);
builder.Services.AddOcelot()
    .AddCustomLoadBalancer<QueryBasedLoadBalancer>((_, _, provider) => new(provider.GetAsync));


var trustedUrls = builder.Configuration.GetSection("CorsOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(trustedUrls)
              .WithMethods("GET")
              .WithHeaders("Content-Type");
    });
});

var app = builder.Build();

app.UseCors();

app.MapDefaultEndpoints();

await app.UseOcelot();

app.Run();
