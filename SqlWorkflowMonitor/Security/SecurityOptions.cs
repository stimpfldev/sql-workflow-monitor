namespace SqlWorkflowMonitor.Security;

public sealed class SecurityOptions
{
    public const string SectionName = "Security";

    public bool RequireHttps { get; set; }

    public string ApiKey { get; set; } = string.Empty;

    public AdminSecurityOptions Admin { get; set; } = new();
}

public sealed class AdminSecurityOptions
{
    public string Username { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public string PasswordSalt { get; set; } = string.Empty;
}
