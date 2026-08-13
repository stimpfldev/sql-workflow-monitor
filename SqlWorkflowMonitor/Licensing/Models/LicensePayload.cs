using System.Text.Json.Serialization;

namespace SqlWorkflowMonitor.Licensing.Models;

public sealed class LicensePayload
{
    [JsonPropertyName("product")]
    public string Product { get; init; } =
        LicenseConstants.ProductName;

    [JsonPropertyName("licenseId")]
    public Guid LicenseId { get; init; }

    [JsonPropertyName("customer")]
    public string Customer { get; init; } = string.Empty;

    [JsonPropertyName("edition")]
    public string Edition { get; init; } = string.Empty;

    [JsonPropertyName("installationId")]
    public Guid InstallationId { get; init; }

    [JsonPropertyName("issuedUtc")]
    public DateTimeOffset IssuedUtc { get; init; }

    [JsonPropertyName("expiresUtc")]
    public DateTimeOffset ExpiresUtc { get; init; }

    [JsonPropertyName("maxProcesses")]
    public int MaxProcesses { get; init; }

    [JsonPropertyName("maxWorkers")]
    public int MaxWorkers { get; init; }

    [JsonPropertyName("features")]
    public List<string> Features { get; init; } = [];

    [JsonPropertyName("schemaVersion")]
    public int SchemaVersion { get; init; } =
        LicenseConstants.SchemaVersion;
}