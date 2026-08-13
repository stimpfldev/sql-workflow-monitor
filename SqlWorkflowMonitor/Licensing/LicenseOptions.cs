namespace SqlWorkflowMonitor.Licensing;

public sealed class LicenseOptions
{
    public const string SectionName = "Licensing";

    public const string DefaultLicenseFilePath =
        "License/sqlworkflowmonitor.lic";

    public const string DefaultPublicKeyFilePath =
        "Licensing/Keys/sqlworkflowmonitor-public.pem";

    public string LicenseFilePath { get; init; } =
        DefaultLicenseFilePath;

    public string PublicKeyFilePath { get; init; } =
        DefaultPublicKeyFilePath;
}