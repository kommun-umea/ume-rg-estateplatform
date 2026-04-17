# ume-rg-estateplatform

A .NET solution developed by Umeå Kommun for interfacing with the Pythagoras API data.

## Overview

This project provides a API layer that communicates with the Pythagoras system to access and manage data. It serves as a bridge between Umeå Kommun's internal systems and the Pythagoras API, enabling streamlined data retrieval and processing.

## Technology Stack

- **.NET 10 / ASP.NET Core**
- **Entity Framework Core** - Persistence layer
- **Pythagoras API** - External data source integration
- **Azure Blob Storage** - Image and file storage
- **Azure OpenAI** - AI-assisted features
- **FusionCache** - Distributed L1/L2 caching
- **Polly / Microsoft.Extensions.Http.Resilience** - Retry and circuit breaker policies
- **JWT Bearer Authentication** - API authentication
- **Swagger / Swashbuckle** - OpenAPI documentation
- **Application Insights / OpenTelemetry** - Telemetry
- **xUnit + Shouldly** - Testing
- **Bicep** - Infrastructure as Code
- **Azure DevOps** - CI/CD Pipelines

## Project Structure

```
src/ume-app-estateservice/
├── Umea.se.EstateService.API/           # Main API project (ASP.NET Core)
├── Umea.se.EstateService.Logic/         # Business logic layer
├── Umea.se.EstateService.ServiceAccess/ # External service integration (Pythagoras)
├── Umea.se.EstateService.DataStore/     # EF Core persistence layer
├── Umea.se.EstateService.Shared/        # Shared utilities and models
├── Umea.se.EstateService.Test/          # Unit and integration tests
├── Umea.se.Toolkit.Images/              # Shared image processing library
└── docs/                                # Additional API documentation
iac/                                     # Infrastructure as Code (Bicep)
pipelines/                               # Azure DevOps pipeline definitions
```

## Getting Started

### Prerequisites

- .NET 10 SDK
- IDE of choice (Visual Studio, JetBrains Rider, or Visual Studio Code)
- Access to Pythagoras API credentials
- Access to an Azure Key Vault with the required secrets (see Configuration)
- Access to Umeå Kommun's Azure DevOps NuGet feeds might make things easier. As of writing this is not public (yet)

### Installation

1. Clone the repository:
```bash
git clone https://github.com/[organization]/ume-rg-estateplatform.git
cd ume-rg-estateplatform
```

2. Restore dependencies:
```bash
dotnet restore
```

3. Configure NuGet package sources:
   - The project uses custom NuGet feeds from Umeå Kommun's Azure DevOps
   - Ensure you have access to the required package sources (see `NuGet.Config`)

4. Configure your application settings:
   - Update `appsettings.json` with your Pythagoras API configuration
   - Set up any required connection strings and API keys

5. Build and run the application:
```bash
dotnet build
dotnet run --project src/ume-app-estateservice/Umea.se.EstateService.API
```

## Configuration

Most secrets are resolved from Azure Key Vault via the `@KeyVault(...)` placeholders in `appsettings.json`. Before running the application, ensure you have configured:

- **Key Vault**: `KeyVaultUrl` and credentials (DefaultAzureCredential) with access to the required secrets
- **Database**: `ConnectionStrings:EstateService` for the EF Core data store
- **Pythagoras API**: `Pythagoras:ApiKey` and `Pythagoras:BaseUrl`
- **Authentication**: `Authentication:TokenServiceUrl` and `Authentication:Audience` for JWT bearer auth, plus `Api:Keys`
- **Azure OpenAI**: `OpenAI:Endpoint`, `OpenAI:Model`, and `OpenAI:Enabled`
- **Image cache**: `ImageCache:BlobServiceUrl` and `ImageCache:BlobContainerName`
- **Work orders**: `WorkOrder:FileStorage`, `WorkOrder:FileStorageContainer`, and processing/validation settings
- **Data sync**: `DataSync:Schedule` cron expressions
- **Application Insights**: `ApplicationInsights:ConnectionString`
- **CORS**: `Cors:AllowedOrigins` for your front-end origins
- **NuGet Authentication**: Access to Umeå Kommun's private package feeds

> **Note**: The Infrastructure as Code (IaC) and CI/CD pipelines included in this repository are specifically configured for Umeå Kommun's deployment environment. If you plan to use this solution in a different environment, you will need to configure your own deployment setup and Key Vault.

Additional API documentation lives in [`src/ume-app-estateservice/docs/`](src/ume-app-estateservice/docs/).

## Development Environment

### Package Sources

The project uses multiple NuGet package sources:
- **nuget.org**: Public NuGet packages
- **Umea.se**: Umeå Kommun's internal package feed
- **turkos.umea.se**: Additional internal packages

Ensure you have proper authentication configured for the private feeds.

## Contributing

We welcome contributions to improve this project. Please follow these guidelines:

### Pull Request Process

- **Target Branch**: All pull requests must be made to the `main` branch
- **Squash Commits**: All commits will be squashed when merging to maintain a clean commit history
- **Code Review**: All pull requests require review before merging

### Workflow

1. Create a feature branch from `main`
2. Make your changes following the established coding standards (see `.editorconfig`)
3. Ensure all tests pass and code analysis rules are satisfied
4. Commit with clear, descriptive messages
5. Open a pull request targeting the `main` branch
6. Address any feedback from code review
7. Once approved, your PR will be squashed and merged

## Code Quality

This project follows strict code quality standards enforced through:

- **EditorConfig**: Consistent code formatting and style rules
- **Code Analysis**: Extensive CA (Code Analysis) rules for best practices
- **SonarLint**: Additional static code analysis is prefered
- **File-scoped namespaces**: Modern C# namespace declarations
- **Warnings = Errors**: We treat warnings as errors

Key coding standards:
- Explicit type declarations preferred over `var` (enforced as a warning, which fails the build under warnings-as-errors)
- Mandatory curly braces for all code blocks
- File-scoped namespace declarations
- Comprehensive CA rules for performance and maintainability


## Deployment

> **Important**: The deployment infrastructure included in this repository is tailored for Umeå Kommun's specific environment. External users must configure their own:
> - Infrastructure as Code (IaC) templates
> - CI/CD pipelines
> - Environment configurations
> - Security settings
> - Keyvaults and secrets

## Support

For questions or issues related to this project, please:

1. Check existing issues in the repository
2. Create a new issue with detailed information about your problem

## License

This project is licensed under the AGPL-3.0 License - see the [LICENSE](LICENSE) file for details.

Copyright (c) 2025 Umea Kommun

## Team

Developed and maintained by Team Turkos at Umeå Kommun.

---
 
