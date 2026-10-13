using Ate.Api;
using Ate.Api.Extensions;
using Ate.Application;
using Ate.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApiServices();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

await app.InitialiseDatabaseAsync();

app.UseScalarDocumentation();

app.MapGet("/api/health", () => Results.Ok(new { status = "Healthy", timestamp = DateTimeOffset.UtcNow }))
   .WithName("HealthCheck")
   .WithTags("System Health")
   .WithSummary("System health check")
   .WithDescription("Returns API operational status and current server timestamp.");

app.Run();
