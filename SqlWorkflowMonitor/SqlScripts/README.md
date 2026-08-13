# Database versioning

SqlWorkflowMonitor treats the database as code. Schema objects, stored procedures, constraints, seed data, and licensing support are co-versioned with the application through ordered, incremental SQL scripts.

## Model

- `001_...sql` through `NNN_...sql` are the authoritative migration history.
- Applied numbered scripts must not be rewritten after a published release. Corrections should be introduced in a new numbered script.
- `Install.sql` is a generated convenience installer for a clean database. It is not the authoritative source.
- The public repository defaults to `SqlWorkflowMonitor_Dev`. Release packaging generates a production installer for `SqlWorkflowMonitor`.

This approach is commonly described as **Database as Code**, **schema versioning**, or **script-based incremental migrations**. It is similar in principle to Flyway, Liquibase, DbUp, and RoundhousE, although this repository currently uses PowerShell-based generation rather than a migration runtime.

## Build and verification

From the repository root:

```powershell
./scripts/Build-InstallSql.ps1
./scripts/Build-InstallSql.ps1 -VerifyOnly
```

To generate an installer for a different database name:

```powershell
./scripts/Build-InstallSql.ps1 `
  -DatabaseName SqlWorkflowMonitor `
  -OutputPath release-output/Install.sql
```

Database names are restricted to letters, digits, and underscores.

## Upgrade policy

`Install.sql` is intended for clean installations. An upgrade of an existing installation must execute only the numbered scripts that have not yet been applied. Before commercial deployment, record applied migrations in an operational change log or a dedicated schema-history table and back up the database.
