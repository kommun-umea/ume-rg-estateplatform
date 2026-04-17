# Architecture

## Overview

ASP.NET Core API that brokers data between Umeå Kommun's internal systems and the **Pythagoras** facility-management system. Single deployable, layered solution. Persistence via EF Core (SQL Server in deployed envs, SQLite for local/integration). Outbound HTTP to Pythagoras is wrapped in `Microsoft.Extensions.Http.Resilience` (Polly-based retry + circuit breaker). FusionCache provides L1 (memory) + L2 (Azure Blob) caching for images and other heavy reads.

## Solution Layout

```
src/ume-app-estateservice/
├── Umea.se.EstateService.API/           # ASP.NET Core entry point, controllers, filters
├── Umea.se.EstateService.Logic/         # Business logic, handlers, search engine
├── Umea.se.EstateService.ServiceAccess/ # Outbound integrations (Pythagoras, file storage)
├── Umea.se.EstateService.DataStore/     # EF Core DbContext + migrations
├── Umea.se.EstateService.Shared/        # Shared models, value objects, configuration
├── Umea.se.EstateService.Test/          # Unit + integration tests
└── Umea.se.Toolkit.Images/              # Image processing/caching helpers
```

`API → Logic → (DataStore | ServiceAccess) → Shared`. Logic never references API; ServiceAccess and DataStore never reference each other.

## Key Modules

- **Search** (`Logic/Search/`): in-memory full-text + n-gram + SymSpell-fuzzy + geospatial search over `PythagorasDocument`. See `SEARCH.MD` at the repo root for indexing, scoring, and tunables.
- **WorkOrder** (`Logic/Handlers/WorkOrder/`): create/update/sync work orders into Pythagoras, including LLM-based category classification (`Azure.AI.OpenAI` via `Microsoft.Extensions.AI`).
- **Image cache** (`Umea.se.Toolkit.Images` + `Logic/.../BuildingImageService`): pre-warms and serves Pythagoras imagery from a blob-backed FusionCache.
- **DataSync** (cron-driven, see `DataSync:Schedule` in `appsettings.json`): periodic refresh of cached document and document-listing data.

## Cross-Cutting

- **Configuration**: `ApplicationConfig` wraps `IConfiguration`. Secrets resolve from Azure Key Vault via `@KeyVault(...)` placeholders. `DefaultAzureCredential` is the default identity for Azure SDKs.
- **Authentication**: JWT bearer against Umeå's internal token service. Audience configured via `Authentication:Audience`.
- **Feature flags**: `EstateServiceFeatureGateMiddleware` reads `Features` (comma-separated string) to gate endpoints.
- **Telemetry**: Application Insights via `Umea.se.Toolkit` default loggers (skipped in `IntegrationTest` environment).
- **Resilience**: three named HTTP clients to Pythagoras (`Pythagoras`, `PythagorasImages`, `PythagorasBlueprints`) with progressively higher timeouts to match cache budgets.

## Domain Terms

- **Estate / Building / Room** — `NodeType` levels of the Pythagoras hierarchy (see `Shared/Search/NodeType.cs`).
- **Pythagoras** — the upstream facility-management system; the API for it is the only external dependency.
- **Document** in search context (`PythagorasDocument`) ≠ document in WorkOrder context (uploaded files).
- **Action type** (in WorkOrder) — Pythagoras classification IDs (`DocumentActionTypeId`, `DocumentActionTypeStatusId`).
