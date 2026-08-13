using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.RateLimiting;
using SqlWorkflowMonitor.Data;
using SqlWorkflowMonitor.Infrastructure;
using SqlWorkflowMonitor.Licensing;
using SqlWorkflowMonitor.Licensing.Models;
using SqlWorkflowMonitor.Licensing.Services;
using SqlWorkflowMonitor.Security;
using SqlWorkflowMonitor.Services;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options =>
{
    options.AddServerHeader = false;
});

SecurityConfigurationValidator.Validate(builder.Configuration);

bool requireHttps =
    builder.Configuration.GetValue<bool>(
        "Security:RequireHttps");

builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "SqlWorkflowMonitor Web";
});

builder.Services.AddLocalization(options =>
{
    options.ResourcesPath = "Resources";
});

builder.Services
    .AddControllersWithViews()
    .AddViewLocalization(
        LanguageViewLocationExpanderFormat.Suffix)
    .AddDataAnnotationsLocalization();

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor |
        ForwardedHeaders.XForwardedProto;

    options.ForwardLimit = 1;
});

builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    CultureInfo[] supportedCultures =
    [
        new("es-AR"),
        new("en-US")
    ];

    options.DefaultRequestCulture =
        new RequestCulture("es-AR");

    options.SupportedCultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;
});

builder.Services.Configure<SecurityOptions>(
    builder.Configuration.GetSection(
        SecurityOptions.SectionName));

builder.Services.Configure<LicenseOptions>(
    builder.Configuration.GetSection(
        LicenseOptions.SectionName));

builder.Services.AddOpenApi();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme =
            CookieAuthenticationDefaults.AuthenticationScheme;

        options.DefaultChallengeScheme =
            CookieAuthenticationDefaults.AuthenticationScheme;
    })
    .AddCookie(
        CookieAuthenticationDefaults.AuthenticationScheme,
        options =>
        {
            options.LoginPath = "/account/login";
            options.AccessDeniedPath = "/account/access-denied";
            options.Cookie.Name = "SqlWorkflowMonitor.Admin";
            options.Cookie.HttpOnly = true;
            options.Cookie.IsEssential = true;
            options.Cookie.SameSite = SameSiteMode.Strict;
            options.Cookie.SecurePolicy = requireHttps
                ? CookieSecurePolicy.Always
                : CookieSecurePolicy.SameAsRequest;
            options.ExpireTimeSpan = TimeSpan.FromHours(8);
            options.SlidingExpiration = true;
        })
    .AddScheme<
        AuthenticationSchemeOptions,
        ApiKeyAuthenticationHandler>(
            ApiKeyAuthenticationHandler.SchemeName,
            _ =>
            {
            });

builder.Services.AddAuthorization();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode =
        StatusCodes.Status429TooManyRequests;

    options.AddPolicy(
        "login",
        httpContext =>
            RateLimitPartition.GetFixedWindowLimiter(
                httpContext.Connection.RemoteIpAddress?
                    .ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 5,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0,
                    QueueProcessingOrder =
                        QueueProcessingOrder.OldestFirst,
                    AutoReplenishment = true
                }));

    options.AddPolicy(
        "api",
        httpContext =>
            RateLimitPartition.GetFixedWindowLimiter(
                httpContext.Connection.RemoteIpAddress?
                    .ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 120,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0,
                    QueueProcessingOrder =
                        QueueProcessingOrder.OldestFirst,
                    AutoReplenishment = true
                }));

    options.AddPolicy(
        "health",
        httpContext =>
            RateLimitPartition.GetFixedWindowLimiter(
                httpContext.Connection.RemoteIpAddress?
                    .ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 30,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0,
                    QueueProcessingOrder =
                        QueueProcessingOrder.OldestFirst,
                    AutoReplenishment = true
                }));
});

builder.Services.AddSingleton<SqlConnectionFactory>();
builder.Services.AddScoped<ExecutionRepository>();
builder.Services.AddScoped<ProductAccessRepository>();
builder.Services.AddScoped<
    IProductAccessService,
    ProductAccessService>();

builder.Services.AddSingleton<
    ILicenseFileReader,
    LicenseFileReader>();

builder.Services.AddSingleton<
    ILicenseSignatureVerifier,
    LicenseSignatureVerifier>();

builder.Services.AddScoped<
    ILicenseValidator,
    LicenseValidator>();

var app = builder.Build();

app.UseForwardedHeaders();
app.UseExceptionHandler();
app.UseMiddleware<SecurityHeadersMiddleware>();

if (requireHttps)
{
    if (!app.Environment.IsDevelopment())
    {
        app.UseHsts();
    }

    app.UseHttpsRedirection();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint(
            "/openapi/v1.json",
            "SQL Workflow Monitor API v1");
    });
}

app.UseStaticFiles();
app.UseRequestLocalization();
app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

if (app.Environment.IsDevelopment())
{
    app.MapGet(
        "/api/license/validation",
        async (
            ILicenseValidator validator,
            CancellationToken cancellationToken) =>
        {
            LicenseValidationResult result =
                await validator.ValidateAsync(
                    cancellationToken);

            return Results.Ok(new
            {
                State = result.State.ToString(),
                Edition = result.License?.Edition,
                Customer = result.License?.Customer,
                result.DaysRemaining,
                result.GraceDaysRemaining,
                result.IsExpiringSoon,
                result.CanStartExecutions,
                result.IsReadOnly,
                result.Error
            });
        });
}

app.MapGet(
    "/",
    () => Results.Redirect("/executions"));

app.Run();
