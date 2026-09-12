using System.Text;
using Microsoft.AspNetCore.Builder;

namespace FreshCart.CustomerSupport.Api;

/// <summary>
/// Development-only Swagger UI wiring. The document remains the ASP.NET Core OpenAPI document, while
/// this UI package supplies only static browser assets and a self-hosted initializer.
/// </summary>
internal static class DevelopmentSwaggerUi
{
    private const string SwaggerCsp =
        "default-src 'none'; script-src 'self'; style-src 'self'; img-src 'self' data:; "
        + "connect-src 'self'; font-src 'self'; frame-ancestors 'none'; base-uri 'none'; form-action 'self'";

    private const string IndexHtml = """
        <!DOCTYPE html>
        <html lang="en">
        <head>
            <meta charset="UTF-8">
            <meta name="viewport" content="width=device-width, initial-scale=1">
            <title>FreshCart Customer Support API</title>
            <link rel="stylesheet" href="./swagger-ui.css">
            <link rel="icon" type="image/png" href="./favicon-32x32.png" sizes="32x32">
            <link rel="icon" type="image/png" href="./favicon-16x16.png" sizes="16x16">
        </head>
        <body>
            <div id="swagger-ui"></div>
            <script src="./swagger-ui-bundle.js" charset="utf-8"></script>
            <script src="./swagger-ui-standalone-preset.js" charset="utf-8"></script>
            <script src="./index.js" charset="utf-8"></script>
        </body>
        </html>
        """;

    public static void Use(IApplicationBuilder application)
    {
        ArgumentNullException.ThrowIfNull(application);

        application.Use(async (httpContext, next) =>
        {
            if (httpContext.Request.Path.StartsWithSegments("/swagger", StringComparison.OrdinalIgnoreCase))
            {
                httpContext.Response.OnStarting(() =>
                {
                    httpContext.Response.Headers["Content-Security-Policy"] = SwaggerCsp;
                    return Task.CompletedTask;
                });
            }

            await next().ConfigureAwait(false);
        });

        application.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/openapi/v1.json", "FreshCart Customer Support API");
            options.ConfigObject.ValidatorUrl = null;
            options.DocumentTitle = "FreshCart Customer Support API";
            options.IndexStream = () => new MemoryStream(Encoding.UTF8.GetBytes(IndexHtml));
        });
    }
}
