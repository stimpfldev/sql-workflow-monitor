using SqlWorkflowMonitor.Worker;
using SqlWorkflowMonitor.Worker.Data;
using SqlWorkflowMonitor.Worker.Services;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "SqlWorkflowMonitor Worker";
});

string baseUrl =
    builder.Configuration["WorkflowMonitorApi:BaseUrl"]
    ?? throw new InvalidOperationException(
        "No se encontró WorkflowMonitorApi:BaseUrl.");

string apiKey =
    builder.Configuration["WorkflowMonitorApi:ApiKey"]
    ?? throw new InvalidOperationException(
        "No se encontró WorkflowMonitorApi:ApiKey.");

if (string.IsNullOrWhiteSpace(apiKey))
{
    throw new InvalidOperationException(
        "WorkflowMonitorApi:ApiKey no puede estar vacía.");
}

builder.Services.AddHttpClient(
    "WorkflowMonitorApi",
    client =>
    {
        client.BaseAddress = new Uri(baseUrl);

        client.DefaultRequestHeaders.Add(
            "X-Api-Key",
            apiKey);
    });

builder.Services.AddSingleton<CustomerCsvProcessor>();

builder.Services.AddSingleton<
    StagingCustomerRepository>();

builder.Services.AddHostedService<Worker>();

var host = builder.Build();

host.Run();