# SqlWorkflowMonitor 1.1.1

Release date: August 5, 2026

Version 1.1.1 is a maintenance release. It does not change the product's core execution-monitoring scope. It aligns source, configuration, SQL installation, packaging, security, documentation, legal material, and release materials.

## Main corrections

- Complete SQL installer through migration `011`.
- Correct nested administrator configuration.
- Consistent `403 Forbidden` behavior for product-access restrictions.
- Loopback-only production binding by default.
- Encrypted production SQL connection template.
- Rate limiting, CSP, defensive browser headers, and fail-fast secret validation.
- CSV resource and path controls.
- CI, repository verification, Dependabot, and release governance.
- Removal of executable packages and machine-specific files from source control.

## Upgrade note

Existing `1.1.0` databases should apply only incremental scripts not previously executed. Back up the database and record applied migrations before upgrading. `Install.sql` is intended for clean installations.

## Distribution

The public source repository is proprietary source-available evaluation material. Executable Demo packages are distributed separately as GitHub Release assets and are subject to the applicable license terms.
