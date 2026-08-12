namespace SqlWorkflowMonitor.Infrastructure;

public sealed class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IHostEnvironment _environment;

    public SecurityHeadersMiddleware(
        RequestDelegate next,
        IHostEnvironment environment)
    {
        _next = next;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(() =>
        {
            IHeaderDictionary headers = context.Response.Headers;

            headers.TryAdd("X-Content-Type-Options", "nosniff");
            headers.TryAdd("X-Frame-Options", "DENY");
            headers.TryAdd("Referrer-Policy", "no-referrer");
            headers.TryAdd("Cross-Origin-Opener-Policy", "same-origin");
            headers.TryAdd("Cross-Origin-Resource-Policy", "same-origin");
            headers.TryAdd(
                "Permissions-Policy",
                "camera=(), microphone=(), geolocation=(), payment=()");

            bool developmentOpenApi =
                _environment.IsDevelopment() &&
                (context.Request.Path.StartsWithSegments("/swagger") ||
                 context.Request.Path.StartsWithSegments("/openapi"));

            string contentSecurityPolicy = developmentOpenApi
                ? "default-src 'self'; " +
                  "script-src 'self' 'unsafe-inline'; " +
                  "style-src 'self' 'unsafe-inline'; " +
                  "img-src 'self' data:; " +
                  "font-src 'self'; " +
                  "connect-src 'self'; " +
                  "frame-ancestors 'none'; " +
                  "base-uri 'self'; form-action 'self'; object-src 'none'"
                : "default-src 'self'; " +
                  "script-src 'self'; style-src 'self'; " +
                  "img-src 'self' data:; font-src 'self'; " +
                  "connect-src 'self'; frame-ancestors 'none'; " +
                  "base-uri 'self'; form-action 'self'; object-src 'none'";

            headers.TryAdd(
                "Content-Security-Policy",
                contentSecurityPolicy);

            if (context.Request.Path.StartsWithSegments("/api") ||
                context.Request.Path.StartsWithSegments("/account") ||
                context.Request.Path.StartsWithSegments("/executions"))
            {
                headers.TryAdd(
                    "Cache-Control",
                    "no-store, no-cache, must-revalidate");
                headers.TryAdd("Pragma", "no-cache");
            }

            return Task.CompletedTask;
        });

        await _next(context);
    }
}
