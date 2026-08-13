# Architecture

SqlWorkflowMonitor uses a **modular layered monolith with a companion Worker Service and a shared SQL Server database**.

## Runtime components

1. **Web and API host** â€” ASP.NET Core MVC dashboard, REST API, authentication, authorization, licensing policy, operational queries, localization, and CSV export.
2. **Worker Service** â€” background CSV ingestion, validation, staging, stored-procedure execution, and execution lifecycle reporting through the API.
3. **SQL Server database** â€” process definitions, execution history, metrics, staging tables, consistency constraints, stored procedures, and installation/licensing state.
4. **Offline license verification** — the application validates signed license files locally using public-key cryptography and does not require Internet access.

## Internal layers

- **Presentation:** MVC controllers, Razor views, localized resources, API controllers.
- **Application services:** product-access policy and execution orchestration.
- **Infrastructure:** SQL repositories, authentication handlers, security middleware, exception handling, license file and signature readers.
- **Database:** versioned SQL scripts and stored procedures.

The repository uses the Repository and Service Layer patterns, dependency injection, DTOs, typed configuration, background-worker processing, and parameterized ADO.NET commands.

## Architectural boundaries

The solution is not presented as microservices, CQRS, DDD, Clean Architecture, or Onion Architecture. The selected structure is deliberate: it keeps an on-premises product deployable as two Windows services without introducing distributed-system complexity that the current product does not require.

## Main flow

1. A Worker or integration authenticates with an API key.
2. It requests the start of an execution.
3. The application validates Demo or signed-license policy.
4. SQL Server creates the execution and enforces consistency.
5. The Worker performs the operation and reports final metrics.
6. An authenticated administrator reviews status, duration, errors, stale executions, and exports from the dashboard.

## Trust boundaries

- The dashboard requires a local administrator cookie.
- API integrations require `X-Api-Key`.
- The public health endpoint exposes only availability state.
- The default packaged binding is loopback-only.
- Remote access must be terminated through HTTPS at a trusted reverse proxy or explicitly configured HTTPS endpoint.
- Sensitive cryptographic and customer-specific material must not be stored in the repository.
