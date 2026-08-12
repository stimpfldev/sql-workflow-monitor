using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Extensions.Configuration;
using SqlWorkflowMonitor.Worker.Configuration;

namespace SqlWorkflowMonitor.Tests.Unit;

[TestClass]
public sealed class WorkerConfigurationValidatorTests
{
    [TestMethod]
    public void Validate_WithLoopbackHttpAndDistinctFolders_DoesNotThrow()
    {
        IConfiguration configuration = CreateConfiguration(
            baseUrl: "http://127.0.0.1:5080",
            inputFolder: "Input",
            processedFolder: "Processed",
            errorFolder: "Error");

        WorkerConfigurationValidator.Validate(configuration);
    }

    [TestMethod]
    public void Validate_WithRemoteHttp_RequiresHttps()
    {
        IConfiguration configuration = CreateConfiguration(
            baseUrl: "http://monitor.internal:5080",
            inputFolder: "Input",
            processedFolder: "Processed",
            errorFolder: "Error");

        InvalidOperationException exception =
            Assert.ThrowsExactly<InvalidOperationException>(
                () => WorkerConfigurationValidator.Validate(configuration));

        StringAssert.Contains(
            exception.Message,
            "Use HTTPS para hosts remotos");
    }

    [TestMethod]
    public void Validate_WithRepeatedFolders_ThrowsClearError()
    {
        IConfiguration configuration = CreateConfiguration(
            baseUrl: "https://monitor.internal",
            inputFolder: "Data",
            processedFolder: "Data",
            errorFolder: "Error");

        InvalidOperationException exception =
            Assert.ThrowsExactly<InvalidOperationException>(
                () => WorkerConfigurationValidator.Validate(configuration));

        StringAssert.Contains(
            exception.Message,
            "deben ser diferentes");
    }

    private static IConfiguration CreateConfiguration(
        string baseUrl,
        string inputFolder,
        string processedFolder,
        string errorFolder)
    {
        var values = new Dictionary<string, string?>
        {
            ["WorkflowMonitorApi:BaseUrl"] = baseUrl,
            ["WorkflowMonitorApi:ApiKey"] = new string('b', 32),
            ["WorkflowMonitorApi:TimeoutSeconds"] = "30",
            ["Worker:Id"] = "CustomerCsvWorker",
            ["CsvImport:ProcessId"] = "1",
            ["CsvImport:FileName"] = "customers.csv",
            ["CsvImport:InputFolder"] = inputFolder,
            ["CsvImport:ProcessedFolder"] = processedFolder,
            ["CsvImport:ErrorFolder"] = errorFolder,
            ["CsvImport:IntervalSeconds"] = "60",
            ["CsvImport:MaxFileSizeBytes"] = "10485760",
            ["CsvImport:MaxRows"] = "100000",
            ["CsvImport:MinimumFileAgeSeconds"] = "5"
        };

        return new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
    }

}
