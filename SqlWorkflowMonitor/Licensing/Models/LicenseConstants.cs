namespace SqlWorkflowMonitor.Licensing.Models;

public static class LicenseConstants
{
    public const string ProductName = "SqlWorkflowMonitor";
    public const int SchemaVersion = 1;

    public const int ExpirationWarningDays = 30;
    public const int GracePeriodDays = 14;
}

public static class LicenseEditions
{
    public const string Professional = "Professional";
    public const string Enterprise = "Enterprise";
}

public static class LicenseFeatures
{
    public const string Dashboard = "dashboard";
    public const string Api = "api";
    public const string CsvExport = "csv-export";
}