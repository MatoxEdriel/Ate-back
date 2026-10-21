using Scalar.AspNetCore;

namespace Ate.Api.Extensions;

public static class ScalarExtensions
{
    public static WebApplication UseScalarDocumentation(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
            app.MapScalarApiReference(options =>
            {
                options.WithTitle("Ate API Reference")
                       .WithTheme(ScalarTheme.Moon)
                       .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
            });

            app.MapGet("/", () => Results.Redirect("/scalar/v1")).ExcludeFromDescription();
            app.MapGet("/scalar", () => Results.Redirect("/scalar/v1")).ExcludeFromDescription();
        }

        return app;
    }
}
