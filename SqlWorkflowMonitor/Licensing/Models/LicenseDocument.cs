using System.Text.Json.Serialization;

namespace SqlWorkflowMonitor.Licensing.Models;

public sealed class LicenseDocument
{
    [JsonPropertyName("license")]
    public LicensePayload License { get; init; } = new();

    [JsonPropertyName("signature")]
    public string Signature { get; init; } = string.Empty;
}