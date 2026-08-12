namespace SqlWorkflowMonitor.Security;

public static class SecurityConfigurationValidator
{
    private const int MinimumApiKeyLength = 32;
    private const int ExpectedPasswordHashBytes = 32;
    private const int MinimumPasswordSaltBytes = 16;

    public static void Validate(IConfiguration configuration)
    {
        SecurityOptions options =
            configuration
                .GetSection(SecurityOptions.SectionName)
                .Get<SecurityOptions>()
            ?? throw new InvalidOperationException(
                "No se encontró la sección de configuración 'Security'.");

        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(options.ApiKey) ||
            options.ApiKey.Length < MinimumApiKeyLength)
        {
            errors.Add(
                $"Security:ApiKey debe tener al menos {MinimumApiKeyLength} caracteres.");
        }

        if (string.IsNullOrWhiteSpace(options.Admin.Username))
        {
            errors.Add("Security:Admin:Username es obligatorio.");
        }

        ValidateBase64Value(
            options.Admin.PasswordHash,
            "Security:Admin:PasswordHash",
            ExpectedPasswordHashBytes,
            ExpectedPasswordHashBytes,
            errors);

        ValidateBase64Value(
            options.Admin.PasswordSalt,
            "Security:Admin:PasswordSalt",
            MinimumPasswordSaltBytes,
            int.MaxValue,
            errors);

        if (errors.Count > 0)
        {
            throw new InvalidOperationException(
                "La configuración de seguridad es inválida:" +
                Environment.NewLine +
                string.Join(
                    Environment.NewLine,
                    errors.Select(error => $"- {error}")));
        }
    }

    private static void ValidateBase64Value(
        string value,
        string configurationKey,
        int minimumLength,
        int maximumLength,
        ICollection<string> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add($"{configurationKey} es obligatorio.");
            return;
        }

        try
        {
            byte[] bytes = Convert.FromBase64String(value);

            if (bytes.Length < minimumLength ||
                bytes.Length > maximumLength)
            {
                string expected = minimumLength == maximumLength
                    ? $"exactamente {minimumLength} bytes"
                    : $"al menos {minimumLength} bytes";

                errors.Add(
                    $"{configurationKey} debe representar {expected}.");
            }
        }
        catch (FormatException)
        {
            errors.Add($"{configurationKey} debe ser Base64 válido.");
        }
    }
}
