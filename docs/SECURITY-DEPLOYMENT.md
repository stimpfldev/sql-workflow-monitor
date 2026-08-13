# Secure deployment baseline

SqlWorkflowMonitor is intended for controlled on-premises or private-network deployment. The default production template listens only on `127.0.0.1:5080`.

## Required configuration

Before starting the Web application, configure:

- `ConnectionStrings:WorkflowMonitor`
- `Security:ApiKey` with at least 32 characters
- `Security:Admin:Username`
- `Security:Admin:PasswordHash`
- `Security:Admin:PasswordSalt`

The application fails fast when required security values are missing or malformed.

Before starting the Worker, configure:

- `WorkflowMonitorApi:BaseUrl`
- `WorkflowMonitorApi:ApiKey`
- `ConnectionStrings:WorkflowMonitor`
- Worker identity and CSV folders

The Worker rejects plain HTTP URLs for non-loopback hosts.

## HTTPS and remote access

For a single-machine installation, loopback HTTP between the reverse proxy, Web service, and Worker is acceptable when the machine is trusted and the port is not exposed externally.

For remote access:

1. Keep Kestrel bound to loopback.
2. Place IIS, nginx, Apache, or another controlled reverse proxy in front of the application.
3. Terminate TLS at the proxy using a trusted certificate.
4. Restrict network access and configure the final host name in `AllowedHosts`.
5. Set `Security:RequireHttps` to `true` only when Kestrel itself has a valid HTTPS endpoint or the deployment correctly forwards the original scheme.
6. Configure forwarded headers only for explicitly trusted proxies; do not trust arbitrary forwarded headers from the internet.

## SQL Server

Production templates use encrypted SQL Server connections and do not trust an unverified server certificate. Install a trusted SQL Server certificate or explicitly document and accept any deviation for an isolated environment.

Use a dedicated least-privilege service identity. Database creation and migration permissions should be separated from normal runtime permissions where operationally possible.

## Included protections

- PBKDF2-SHA-256 administrator password verification with 210,000 iterations.
- Constant-time password-hash and API-key comparison.
- HttpOnly, SameSite cookies.
- Login and API rate limiting.
- Anti-forgery protection for browser POST operations.
- Content Security Policy and defensive browser headers.
- Safe Problem Details responses without stack traces.
- Size and row limits for CSV processing.
- Parameterized SQL access and stored procedures.

## Secrets

Do not commit API keys, password hashes, password salts, connection-string passwords, private keys, customer licenses, or production logs. Use .NET User Secrets for local development and an operating-system-protected configuration mechanism for production.
