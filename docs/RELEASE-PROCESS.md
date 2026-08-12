# Release process

The private repository is the canonical source for commercial builds. The public repository is a controlled, source-available evaluation of the same product.

## Rules

- Do not develop independently in both repositories.
- Generate the public repository from the private source through an allowlist/exclusion process.
- Never publish the license generator, private signing keys, issued licenses, customer configuration, or internal release credentials.
- Keep source, SQL scripts, documentation, legal notices, version numbers, and packages aligned.
- Publish executable ZIP files as GitHub Release assets, not as files tracked in the source tree.

## Public release checklist

1. Update the patch/minor version centrally in `Directory.Build.props`.
2. Add a changelog entry.
3. Run `scripts/Build-InstallSql.ps1`.
4. Run `scripts/Verify-PublicRepository.ps1`.
5. Restore and build the solution in Release configuration.
6. Validate a clean database installation.
7. Validate dashboard login, API key, health, Worker processing, Demo limits, invalid-license behavior, CSV export, and Windows Service startup.
8. Generate the self-contained package from the private repository.
9. Verify package contents and SHA-256 checksum.
10. Create a signed or annotated Git tag and a GitHub Release.
11. Attach the package, checksum, and release notes to the GitHub Release.
12. Confirm that the README and documentation point to the current release.

## Public/private synchronization

A release is not complete while either repository contains newer configuration, documentation, SQL, or packaging logic than the other. Differences must be intentional and listed in the publication allowlist.
