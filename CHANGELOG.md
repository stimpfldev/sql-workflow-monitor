# Changelog

All notable public changes to SqlWorkflowMonitor are documented here. Stable releases follow semantic versioning.

## [1.1.1] - 2026-08-05

### Fixed

- Synchronized the consolidated SQL installer with incremental scripts `001` through `011`.
- Corrected the public administrator configuration hierarchy to `Security:Admin:*`.
- Added the required `X-Api-Key` header to manual HTTP requests.
- Mapped product-access and invalid-license restrictions to `403 Forbidden` instead of an unhandled `500` response.
- Removed obsolete ASP.NET template pages, scripts, styles, and user-specific project files.
- Removed the executable ZIP from source control; release binaries are distributed as GitHub Release assets.
- Aligned public release requirements with the hardened private packaging process.

### Security

- Added fail-fast validation for Web and Worker security configuration.
- Added login, API, and health endpoint rate limiting.
- Added Content Security Policy, no-store responses, and defensive browser headers.
- Disabled the Kestrel server header.
- Changed production SQL templates to encrypted connections without trusting an unverified certificate.
- Restricted the packaged Web endpoint to loopback by default and rejected remote Worker endpoints that use plain HTTP.
- Added CSV file-size, row-count, file-age, extension, and path validation.
- Added safe local-return validation for culture changes.

### Engineering and release governance

- Centralized product version metadata in `Directory.Build.props`.
- Added deterministic Database-as-Code installer generation and verification.
- Added GitHub Actions CI builds with warnings treated as errors.
- Added unit tests for Web and Worker security configuration.
- Added Dependabot, issue templates, a pull-request checklist, and contribution guidance.
- Added architecture, secure deployment, database versioning, and release-process documentation.
- Added a repository verification script that rejects secrets, private tooling, licenses, ZIP files, and user-specific artifacts.

### Documentation and release materials

- Clarified the modular layered monolith architecture and companion Worker model.
- Clarified that the public repository is proprietary source-available evaluation material, not open source.
- Aligned security, support, authorship, EULA contact information, and release documentation.
- Updated English and Spanish commercial and installation documents for version `1.1.1`.

## [1.1.0] - 2026-07-28

### Added

- MVC operational dashboard and execution detail views.
- REST execution lifecycle API.
- Filtering, sorting, pagination, stale execution detection, and CSV export.
- Worker Service CSV processing with SQL Server staging and procedures.
- English and Spanish localization.
- Local administrator and API-key authentication.
- Offline signed-license validation and Demo, Professional, and Enterprise policy support.
- Windows Service and self-contained Windows x64 packaging.
- Installation, commercial, and legal documentation.

## [1.0.0] - 2026-07-14

### Added

- Initial execution-monitoring core, SQL Server schema, REST API, dashboard, and Worker integration.
