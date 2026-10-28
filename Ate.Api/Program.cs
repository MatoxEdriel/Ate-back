using Ate.Api;
using Ate.Api.Extensions;
using Ate.Application;
using Ate.Infrastructure;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext());

    const string corsPolicyName = "FrontendCorsPolicy";
    builder.Services.AddCors(options =>
    {
        options.AddPolicy(corsPolicyName, policy =>
        {
            policy.WithOrigins("http://localhost:4200", "http://localhost:3000")
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        });
    });

    builder.Services.AddControllers();
    builder.Services.AddApiServices(builder.Configuration);
    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);

    var app = builder.Build();

    app.UseSerilogRequestLogging();

    app.UseExceptionHandler();

    await app.InitialiseDatabaseAsync();

    app.UseCors(corsPolicyName);
    app.UseAuthentication();
    app.UseAuthorization();

    app.UseScalarDocumentation();

    app.MapControllers();

    app.MapGet("/api/health", () => Results.Ok(new { status = "Healthy", version = "1.0.3", timestamp = DateTimeOffset.UtcNow }))
       .WithName("HealthCheck")
       .WithTags("System Health")
       .WithSummary("System health check")
       .WithDescription("Returns API operational status and current server timestamp.");

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
