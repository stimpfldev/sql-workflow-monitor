# Support

## Repository support

This repository supports technical evaluation, defect reporting, documentation feedback, and authorized product use. GitHub issues may be used for:

- reproducible Demo defects;
- documentation errors;
- clean-installation problems;
- source-build problems;
- SQL installer inconsistencies;
- feature proposals based on a concrete operational use case.

Security reports must follow [SECURITY.md](SECURITY.md) and must not be opened publicly.

## Before opening an issue

1. Use the latest `1.1.x` source or Release.
2. Review the installation guide and [secure deployment baseline](docs/SECURITY-DEPLOYMENT.md).
3. Run `./scripts/Verify-PublicRepository.ps1` for a source checkout.
4. Confirm that the database was created from the synchronized `Install.sql`.
5. Remove all credentials, customer data, production names, license files, and sensitive logs from the report.

## Include

- SqlWorkflowMonitor version;
- affected Web, API, Worker, SQL, packaging, or documentation component;
- Windows and SQL Server versions;
- deployment mode;
- exact reproduction steps;
- expected and actual behavior;
- relevant sanitized logs.

## Support boundaries

Repository support does not include:

- guaranteed response times;
- production incident response;
- customer-specific deployment or migration work;
- license issuance or account-specific commercial operations;
- contractual support obligations;
- custom development without a separate agreement.

## Product inquiries

For product licensing, installation, integration, support, or customization:

- Website: `https://www.federicostimpfl.com.ar`
- Email: `federicosdev@gmail.com`
- LinkedIn: `https://www.linkedin.com/in/federicosdev`
