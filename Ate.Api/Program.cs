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

    // Orígenes permitidos desde configuración (Cors:AllowedOrigins); fuera de Development no hay valor por defecto.
    const string corsPolicyName = "FrontendCorsPolicy";
    var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
    if (allowedOrigins.Length == 0 && !builder.Environment.IsDevelopment())
    {
        throw new InvalidOperationException("Cors:AllowedOrigins must list the frontend origins outside Development.");
    }

    builder.Services.AddCors(options =>
    {
        options.AddPolicy(corsPolicyName, policy =>
        {
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        });
    });

    // Límite de tamaño de las peticiones (el logo en base64 es lo más pesado: ~300 KB).
    builder.WebHost.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = 2 * 1024 * 1024);

    builder.Services.AddControllers(options =>
        options.Filters.Add<Ate.Api.Filters.PasswordChangeRequiredFilter>());
    builder.Services.AddApiServices(builder.Configuration);
    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);

    var app = builder.Build();

    app.UseSerilogRequestLogging();

    app.UseExceptionHandler();

    await app.InitialiseDatabaseAsync();

    app.UseCors(corsPolicyName);
    app.UseAuthentication();
    app.UseRateLimiter();
    app.UseAuthorization();

    app.UseScalarDocumentation();

    app.MapControllers();

    app.MapGet("/api/health", () => Results.Ok(new { status = "Healthy", timestamp = DateTimeOffset.UtcNow }))
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
