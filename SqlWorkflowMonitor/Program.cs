using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc.Razor;
using SqlWorkflowMonitor.Data;
using SqlWorkflowMonitor.Infrastructure;
using SqlWorkflowMonitor.Licensing;
using SqlWorkflowMonitor.Licensing.Models;
using SqlWorkflowMonitor.Licensing.Services;
using SqlWorkflowMonitor.Services;
using System.Globalization;
using Microsoft.AspNetCore.Authentication;
using SqlWorkflowMonitor.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);
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
    .AddViewLocalization(LanguageViewLocationExpanderFormat.Suffix)
    .AddDataAnnotationsLocalization();

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

            options.Cookie.Name =
                "SqlWorkflowMonitor.Admin";

            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite =
                SameSiteMode.Strict;

            options.Cookie.SecurePolicy =
        CookieSecurePolicy.SameAsRequest;

            options.ExpireTimeSpan =
                TimeSpan.FromHours(8);

            options.SlidingExpiration = true;
        })
    .AddScheme<
        AuthenticationSchemeOptions,
        ApiKeyAuthenticationHandler>(
            ApiKeyAuthenticationHandler.SchemeName,
            options =>
            {
            });

builder.Services.AddAuthorization();

builder.Services.AddSingleton<SqlConnectionFactory>();

builder.Services.AddScoped<ExecutionRepository>();
builder.Services.AddScoped<ProductAccessRepository>();

// INICIO MODIFICADO - Acceso real según Demo o licencia
builder.Services.AddScoped<
    IProductAccessService,
    ProductAccessService>();
// FIN MODIFICADO

// INICIO LICENCIAMIENTO
builder.Services.Configure<LicenseOptions>(
    builder.Configuration.GetSection(
        LicenseOptions.SectionName));

builder.Services.AddSingleton<
    ILicenseFileReader,
    LicenseFileReader>();

builder.Services.AddSingleton<
    ILicenseSignatureVerifier,
    LicenseSignatureVerifier>();

builder.Services.AddScoped<
    ILicenseValidator,
    LicenseValidator>();
// FIN LICENCIAMIENTO


var app = builder.Build();

app.UseExceptionHandler();

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
else
{
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRequestLocalization();
app.UseRouting();
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

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapGet("/", () => Results.Redirect("/executions"));

app.Run();
