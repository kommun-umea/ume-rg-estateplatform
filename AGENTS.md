# AGENTS.md

ASP.NET Core API that bridges Umeå Kommun's internal systems and the Pythagoras facility-management API.

## Stack

- C# (latest LTS) on **.NET 10** / ASP.NET Core
- **EF Core 10** (SQL Server in deployed envs, SQLite locally / in integration tests)
- **FusionCache** (memory + Azure Blob L2) for image and read-heavy caching
- **Microsoft.Extensions.Http.Resilience** (Polly) for outbound Pythagoras calls
- **JWT bearer** auth against Umeå's internal token service
- **Azure OpenAI** via `Microsoft.Extensions.AI` for WorkOrder category classification
- **xUnit v3** + **Shouldly** for tests; **Swashbuckle** for OpenAPI
- **Bicep** + **Azure DevOps** pipelines for IaC and CI/CD

Central package versions live in `Directory.Packages.props`. The solution file is `src/ume-app-estateservice/Umea.se.EstateService.slnx`.

## Commands

Build: `dotnet build`
Test all: `dotnet test`
Test single project: `dotnet test src/ume-app-estateservice/Umea.se.EstateService.Test/`
Test single test: `dotnet test --filter "FullyQualifiedName~{ClassName}.{MethodName}"`
Run API: `dotnet run --project src/ume-app-estateservice/Umea.se.EstateService.API`
Clean: `dotnet clean`

The API needs Key Vault access (or local user-secrets) for Pythagoras, OpenAI, blob storage, and Application Insights — see the README "Configuration" section.

## Repo Layout

| Path                                                | Purpose                                               |
| --------------------------------------------------- | ----------------------------------------------------- |
| `src/ume-app-estateservice/Umea.se.EstateService.*` | API, Logic, ServiceAccess, DataStore, Shared, Test    |
| `src/ume-app-estateservice/Umea.se.Toolkit.Images/` | Shared image processing/caching helpers               |
| `src/ume-app-estateservice/docs/`                   | API docs (e.g. `work-orders-api.md`)                  |
| `iac/`                                              | Bicep IaC                                             |
| `pipelines/`                                        | Azure DevOps pipeline definitions                     |
| `SEARCH.MD`                                         | In-memory search engine internals (scoring, indexing) |

## Conventions

- Code style and naming: see `.ai/code-style.md`
- Testing patterns: see `.ai/testing.md`
- Architecture overview: see `.ai/architecture.md`
- Boundaries (always / ask first / never): see `.ai/boundaries.md`
- GitHub Copilot guidance also lives in `.github/copilot-instructions.md` (kept in sync with the above)
