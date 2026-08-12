using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using SqlWorkflowMonitor.Licensing.Models;

namespace SqlWorkflowMonitor.Licensing.Services;

public sealed class LicenseFileReader : ILicenseFileReader
{
    private const long MaxLicenseFileSizeBytes = 64 * 1024;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = false,
        AllowTrailingCommas = false,
        ReadCommentHandling = JsonCommentHandling.Disallow,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        MaxDepth = 16
    };

    private readonly LicenseOptions _options;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<LicenseFileReader> _logger;

    public LicenseFileReader(
        IOptions<LicenseOptions> options,
        IHostEnvironment environment,
        ILogger<LicenseFileReader> logger)
    {
        _options = options.Value;
        _environment = environment;
        _logger = logger;
    }

    public async Task<LicenseFileReadResult> ReadAsync(
        CancellationToken cancellationToken)
    {
        string filePath = GetLicenseFilePath();

        if (!File.Exists(filePath))
        {
            return new LicenseFileReadResult
            {
                State = LicenseFileState.NotFound,
                FilePath = filePath
            };
        }

        try
        {
            FileInfo fileInfo = new(filePath);

            if (fileInfo.Length == 0 ||
                fileInfo.Length > MaxLicenseFileSizeBytes)
            {
                return Invalid(
                    filePath,
                    "The license file is empty or exceeds 64 KB.");
            }

            await using FileStream stream = new(
                filePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read);

            LicenseDocument? document =
                await JsonSerializer.DeserializeAsync<LicenseDocument>(
                    stream,
                    JsonOptions,
                    cancellationToken);

            if (document is null)
            {
                return Invalid(
                    filePath,
                    "The license file has no content.");
            }

            return new LicenseFileReadResult
            {
                State = LicenseFileState.Loaded,
                FilePath = filePath,
                Document = document
            };
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (JsonException exception)
        {
            _logger.LogWarning(
                exception,
                "The license file is not valid JSON: {LicenseFilePath}",
                filePath);

            return Invalid(
                filePath,
                "The license file format is invalid.");
        }
        catch (IOException exception)
        {
            _logger.LogWarning(
                exception,
                "The license file could not be read: {LicenseFilePath}",
                filePath);

            return Invalid(
                filePath,
                "The license file could not be read.");
        }
        catch (UnauthorizedAccessException exception)
        {
            _logger.LogWarning(
                exception,
                "Access to the license file was denied: {LicenseFilePath}",
                filePath);

            return Invalid(
                filePath,
                "Access to the license file was denied.");
        }
    }

    private string GetLicenseFilePath()
    {
        string configuredPath =
            string.IsNullOrWhiteSpace(_options.LicenseFilePath)
                ? LicenseOptions.DefaultLicenseFilePath
                : _options.LicenseFilePath.Trim();

        return Path.IsPathRooted(configuredPath)
            ? Path.GetFullPath(configuredPath)
            : Path.GetFullPath(
                Path.Combine(
                    _environment.ContentRootPath,
                    configuredPath));
    }

    private static LicenseFileReadResult Invalid(
        string filePath,
        string error)
    {
        return new LicenseFileReadResult
        {
            State = LicenseFileState.Invalid,
            FilePath = filePath,
            Error = error
        };
    }
}