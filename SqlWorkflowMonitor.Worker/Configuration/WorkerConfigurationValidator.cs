namespace SqlWorkflowMonitor.Worker.Configuration;

public static class WorkerConfigurationValidator
{
    private const int MinimumApiKeyLength = 32;

    public static void Validate(IConfiguration configuration)
    {
        WorkflowMonitorApiOptions api =
            configuration
                .GetSection(WorkflowMonitorApiOptions.SectionName)
                .Get<WorkflowMonitorApiOptions>()
            ?? new WorkflowMonitorApiOptions();

        CsvImportOptions csv =
            configuration
                .GetSection(CsvImportOptions.SectionName)
                .Get<CsvImportOptions>()
            ?? new CsvImportOptions();

        WorkerIdentityOptions worker =
            configuration
                .GetSection(WorkerIdentityOptions.SectionName)
                .Get<WorkerIdentityOptions>()
            ?? new WorkerIdentityOptions();

        var errors = new List<string>();

        if (!Uri.TryCreate(api.BaseUrl, UriKind.Absolute, out Uri? baseUri) ||
            (baseUri.Scheme != Uri.UriSchemeHttp &&
             baseUri.Scheme != Uri.UriSchemeHttps))
        {
            errors.Add(
                "WorkflowMonitorApi:BaseUrl debe ser una URL HTTP o HTTPS absoluta.");
        }
        else if (baseUri.Scheme == Uri.UriSchemeHttp && !baseUri.IsLoopback)
        {
            errors.Add(
                "WorkflowMonitorApi:BaseUrl solo puede usar HTTP para una dirección loopback. Use HTTPS para hosts remotos.");
        }

        if (string.IsNullOrWhiteSpace(api.ApiKey) ||
            api.ApiKey.Length < MinimumApiKeyLength)
        {
            errors.Add(
                $"WorkflowMonitorApi:ApiKey debe tener al menos {MinimumApiKeyLength} caracteres.");
        }

        if (api.TimeoutSeconds is < 5 or > 300)
        {
            errors.Add(
                "WorkflowMonitorApi:TimeoutSeconds debe estar entre 5 y 300.");
        }

        if (string.IsNullOrWhiteSpace(worker.Id))
        {
            errors.Add("Worker:Id es obligatorio.");
        }

        if (csv.ProcessId <= 0)
        {
            errors.Add("CsvImport:ProcessId debe ser mayor que cero.");
        }

        ValidateRequired(csv.FileName, "CsvImport:FileName", errors);

        if (!string.IsNullOrWhiteSpace(csv.FileName) &&
            (!string.Equals(
                Path.GetFileName(csv.FileName),
                csv.FileName,
                StringComparison.Ordinal) ||
             !string.Equals(
                Path.GetExtension(csv.FileName),
                ".csv",
                StringComparison.OrdinalIgnoreCase)))
        {
            errors.Add(
                "CsvImport:FileName debe ser un nombre de archivo .csv sin ruta.");
        }

        ValidateRequired(csv.InputFolder, "CsvImport:InputFolder", errors);
        ValidateRequired(csv.ProcessedFolder, "CsvImport:ProcessedFolder", errors);
        ValidateRequired(csv.ErrorFolder, "CsvImport:ErrorFolder", errors);

        if (csv.IntervalSeconds is < 5 or > 86_400)
        {
            errors.Add(
                "CsvImport:IntervalSeconds debe estar entre 5 y 86400.");
        }

        if (csv.MaxFileSizeBytes is < 1 or > 1_073_741_824)
        {
            errors.Add(
                "CsvImport:MaxFileSizeBytes debe estar entre 1 byte y 1 GB.");
        }

        if (csv.MaxRows is < 1 or > 5_000_000)
        {
            errors.Add(
                "CsvImport:MaxRows debe estar entre 1 y 5000000.");
        }

        if (csv.MinimumFileAgeSeconds is < 0 or > 3600)
        {
            errors.Add(
                "CsvImport:MinimumFileAgeSeconds debe estar entre 0 y 3600.");
        }

        string[] folders =
        [
            csv.InputFolder,
            csv.ProcessedFolder,
            csv.ErrorFolder
        ];

        if (folders.All(folder => !string.IsNullOrWhiteSpace(folder)))
        {
            try
            {
                string[] fullFolders = folders
                    .Select(Path.GetFullPath)
                    .ToArray();

                if (fullFolders.Distinct(
                        StringComparer.OrdinalIgnoreCase).Count() != 3)
                {
                    errors.Add(
                        "Las carpetas Input, Processed y Error deben ser diferentes.");
                }
            }
            catch (Exception exception)
                when (exception is ArgumentException or
                      NotSupportedException or
                      PathTooLongException)
            {
                errors.Add(
                    "Las rutas configuradas para Input, Processed y Error no son válidas.");
            }
        }

        if (errors.Count > 0)
        {
            throw new InvalidOperationException(
                "La configuración del Worker es inválida:" +
                Environment.NewLine +
                string.Join(
                    Environment.NewLine,
                    errors.Select(error => $"- {error}")));
        }
    }

    private static void ValidateRequired(
        string value,
        string key,
        ICollection<string> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add($"{key} es obligatorio.");
        }
    }
}
