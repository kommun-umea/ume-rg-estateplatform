# Umea RG Estate Platform

## Overview
Umea RG Estate Platform delivers an ASP.NET Core API that surfaces real-estate data aggregated from the external Pythagoras service. It provides REST endpoints for autocomplete, buildings, estates, and workspaces, wrapping upstream responses in domain models suited for municipal workflows.

## Key Features
- Autocomplete endpoint that unifies building and workspace search with in-memory caching
- Building and workspace APIs exposing curated domain models rather than raw Pythagoras DTOs
- Centralised HTTP client per upstream service with API key authentication and base-address management
- Comprehensive unit tests around query builders, service access, and controller behaviour

## Architecture
- **API (`Umea.se.EstateService.API`)**: Hosts controllers, configures dependency injection, Swagger, CORS, and named HTTP clients.
- **Logic (`Umea.se.EstateService.Logic`)**: Holds orchestration services shared across controllers.
- **Service Access (`Umea.se.EstateService.ServiceAccess`)**: Wraps external integrations, including the Pythagoras client, mappers, and strongly typed queries.
- **Shared (`Umea.se.EstateService.Shared`)**: Contains domain models, configuration helpers, and autocomplete scoring utilities.
- **Tests (`Umea.se.EstateService.Test`)**: xUnit test suite covering the service layer, query builder invariants, and controller flows.

## Prerequisites
- .NET SDK 8.0+
- Access to the required configuration secrets (API key and base URL for Pythagoras)

## Setup
1. Restore dependencies: `dotnet restore src/ume-app-estateservice/Umea.se.EstateService.slnx`
2. Build the solution: `dotnet build src/ume-app-estateservice/Umea.se.EstateService.slnx`
3. Ensure configuration secrets are available (user secrets, environment variables, or Azure Key Vault).

## Configuration
`ApplicationConfig` resolves settings from registered configuration sources. Provide the following keys:
- `Pythagoras-Api-Key` — injected as the `api_key` header on outbound requests
- `Pythagoras-Base-Url` — used as the base address for the named `PythagorasHttpClient`

The project template already connects to Azure Key Vault when configured; local development can rely on user secrets or environment variables.

## Running Locally
```bash
dotnet run --project src/ume-app-estateservice/Umea.se.EstateService.API
```
Swagger is enabled by default; browse to `/swagger` to inspect the API surface.

## Testing
```bash
dotnet test src/ume-app-estateservice/Umea.se.EstateService.slnx
```

## Pythagoras Integration
`PythagorasService` centralises outbound calls to the upstream API. It normalises endpoint paths (such as `rest/v1/building` or `rest/v1/workspace`), maps transport DTOs into domain models, and exposes helper methods for autocomplete scenarios that request the maximum allowed result slice before applying local scoring.

### PythagorasQuery
`PythagorasQuery<T>` builds the query strings accepted by Pythagoras endpoints. It offers fluent helpers for general search, filters, ordering, and pagination while guarding against invalid combinations (for example, mixing `Page` with `Skip`).

#### Example: Filtered workspace search
```csharp
await pythagorasClient.GetAsync(
    "rest/v1/workspace",
    query => query
        .GeneralSearch(searchTerm)
        .StartsWith(x => x.Name, searchTerm, caseSensitive: false)
        .Where(x => x.StatusName, Op.Eq, "Active")
        .Take(50)
        .OrderBy(x => x.Name),
    cancellationToken);
```

#### Example: Building lookup by identifiers
```csharp
await pythagorasClient.GetAsync(
    "rest/v1/building",
    query => query
        .WithIds(101, 205, 309)
        .OrderBy(x => x.Name),
    cancellationToken);
```
