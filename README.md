# ume-rg-estateplatform

A .NET solution developed by Umeå Kommun for interfacing with the Pythagoras API data.

## Overview

This project provides a API layer that communicates with the Pythagoras system to access and manage data. It serves as a bridge between Umeå Kommun's internal systems and the Pythagoras API, enabling streamlined data retrieval and processing.

## Technology Stack

- **.NET**
- **ASP.NET**
- **Pythagoras API** - External data source integration
- **Bicep** - Infrastructure as Code
- **Azure DevOps** - CI/CD Pipelines

## Project Structure

```
src/ume-app-estateservice/
├── Umea.se.EstateService.API/          # Main API project
├── Umea.se.EstateService.Logic/        # Business logic layer
├── Umea.se.EstateService.ServiceAccess/# External service integration
├── Umea.se.EstateService.Shared/       # Shared utilities and models
└── Umea.se.EstateService.Test/         # Unit tests
iac/                                    # Infrastructure as Code (Bicep)
pipelines/                              # Azure DevOps pipeline definitions
```

## Getting Started

### Prerequisites

- .NET SDK
- IDE of choice (Visual Studio, JetBrains Rider, or Visual Studio Code)
- Access to Pythagoras API credentials
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

Before running the application, ensure you have configured:

- **Pythagoras API Settings**: Endpoint URLs, authentication credentials
- **Environment Variables**: Any required environment-specific configurations
- **Logging Configuration**: Appropriate logging levels and targets
- **NuGet Authentication**: Access to Umeå Kommun's private package feeds
- **Keyvault

> **Note**: The Infrastructure as Code (IaC) and CI/CD pipelines included in this repository are specifically configured for Umeå Kommun's deployment environment. If you plan to use this solution in a different environment, you will need to configure your own deployment setup and keyvault.

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
- No `var` usage - explicit type declarations required
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

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

Copyright (c) 2025 Umea Kommun

## Team

Developed and maintained by Team Turkos at Umeå Kommun.

---
 
