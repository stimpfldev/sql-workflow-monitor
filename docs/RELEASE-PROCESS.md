# Release process

## Rules

- Keep source, SQL scripts, documentation, legal notices, version numbers, and release packages aligned.
- Do not commit executable release ZIP files to the source repository.
- Validate configuration, database installation, security settings, documentation, and version consistency before publishing a release.
- Publish executable ZIP files as GitHub Release assets.

## Release checklist

1. Update the version centrally in `Directory.Build.props`.
2. Update `VERSION.txt`.
3. Add the corresponding changelog entry.
4. Run `scripts/Build-InstallSql.ps1`.
5. Run `scripts/Verify-PublicRepository.ps1`.
6. Restore and build the solution in Release configuration.
7. Validate a clean database installation.
8. Validate dashboard login, API authentication, health endpoints, Worker processing, product limits, CSV export, and Windows Service startup.
9. Generate the release package.
10. Verify package contents.
11. Generate and verify the SHA-256 checksum.
12. Create the Git tag.
13. Create the GitHub Release.
14. Attach the package and checksum.
15. Confirm that README and documentation reference the current version.

## Completion criteria

A release is complete when:

- source and version information are consistent;
- SQL installation scripts are synchronized;
- automated verification succeeds;
- the solution builds successfully;
- clean installation validation succeeds;
- release assets and checksum are available;
- documentation corresponds to the released version.
