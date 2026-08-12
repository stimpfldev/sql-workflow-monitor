# Security policy

## Supported versions

Security reports are accepted for the latest `1.1.x` release line. Earlier versions should be upgraded before remediation is evaluated.

## Report a vulnerability privately

Do not open a public GitHub issue for a suspected vulnerability.

- Email: `federicosdev@gmail.com`
- Subject: `SqlWorkflowMonitor security report`

Include, when possible:

- affected version and component;
- clear reproduction steps;
- expected and actual behavior;
- potential confidentiality, integrity, or availability impact;
- sanitized evidence that contains no third-party confidential information.

Do not include real passwords, API keys, private keys, license files, connection strings, customer data, production identifiers, or sensitive logs.

## Relevant scope

Reports may cover:

- authentication or authorization bypass;
- API-key exposure or bypass;
- unsafe default configuration;
- sensitive information disclosure;
- SQL injection or database permission issues;
- unsafe file processing or path handling;
- license-validation bypass;
- privilege escalation;
- denial of service with a practical impact;
- dependency vulnerabilities that affect the supported release.

## Deployment responsibility

The application defaults to loopback-only HTTP and requires explicit credentials. Remote exposure must use an appropriately configured TLS reverse proxy or a valid Kestrel HTTPS endpoint. See [docs/SECURITY-DEPLOYMENT.md](docs/SECURITY-DEPLOYMENT.md).

Reports caused solely by intentionally exposing the loopback HTTP template to an untrusted network, committing secrets, disabling documented protections, or granting excessive SQL/Windows permissions may be treated as deployment misconfiguration rather than a product vulnerability.

## Process

Reports are assessed according to severity, reproducibility, supported versions, and practical impact. Acknowledgment, remediation, disclosure, and support timelines are not guaranteed unless a separate written commercial support agreement defines them.

Do not publicly disclose a vulnerability before a fix or mitigation has been prepared and coordinated.

## License boundary

Security research does not grant permission to use the product in production, distribute it, access private signing material, issue unauthorized licenses, or commercially exploit the source. See [LICENSE.txt](LICENSE.txt).
