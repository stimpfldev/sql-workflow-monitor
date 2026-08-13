using Microsoft.Extensions.Options;
using SqlWorkflowMonitor.Worker;
using SqlWorkflowMonitor.Worker.Configuration;
using SqlWorkflowMonitor.Worker.Data;
using SqlWorkflowMonitor.Worker.Services;

var builder = Host.CreateApplicationBuilder(args);

WorkerConfigurationValidator.Validate(builder.Configuration);

builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "SqlWorkflowMonitor Worker";
});

builder.Services.Configure<WorkflowMonitorApiOptions>(
    builder.Configuration.GetSection(
        WorkflowMonitorApiOptions.SectionName));

builder.Services.Configure<CsvImportOptions>(
    builder.Configuration.GetSection(
        CsvImportOptions.SectionName));

builder.Services.Configure<WorkerIdentityOptions>(
    builder.Configuration.GetSection(
        WorkerIdentityOptions.SectionName));

builder.Services.AddHttpClient(
    "WorkflowMonitorApi",
    (serviceProvider, client) =>
    {
        WorkflowMonitorApiOptions options =
            serviceProvider
                .GetRequiredService<
                    IOptions<WorkflowMonitorApiOptions>>()
                .Value;

        client.BaseAddress = new Uri(options.BaseUrl);
        client.Timeout = TimeSpan.FromSeconds(
            options.TimeoutSeconds);
        client.DefaultRequestHeaders.Add(
            "X-Api-Key",
            options.ApiKey);
    });

builder.Services.AddSingleton<CustomerCsvProcessor>();
builder.Services.AddSingleton<StagingCustomerRepository>();
builder.Services.AddHostedService<Worker>();

await builder.Build().RunAsync();
