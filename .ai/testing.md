# Testing

## Frameworks

- **xUnit v3** as the test runner (`xunit` 2.9.3 + `xunit.runner.visualstudio` 3.1.5).
- **Shouldly** for assertions (`Shouldly` 4.3.0).
- **Microsoft.AspNetCore.Mvc.Testing** for in-process API tests via `Umea.se.TestToolkit`.
- **Coverlet** for coverage collection.

## Commands

Run all tests: `dotnet test`
Run a single project: `dotnet test src/ume-app-estateservice/Umea.se.EstateService.Test/`
Run a single test: `dotnet test --filter "FullyQualifiedName~InMemorySearchServiceAddressTests.Search_FindsDocumentsByAddressTokens"`
Run by class: `dotnet test --filter "FullyQualifiedName~InMemorySearchServiceAddressTests"`
With coverage: `dotnet test --collect:"XPlat Code Coverage"`

## Test Structure

- Tests live in `src/ume-app-estateservice/Umea.se.EstateService.Test/`, mirroring the source tree (e.g., `Logic/Search/InMemorySearchService.cs` → `Test/Search/InMemorySearchServiceAddressTests.cs`).
- Integration tests use `TestApiFactory` from `Umea.se.TestToolkit` and run in the `IntegrationTest` ASP.NET environment (see the `IsEnvironment("IntegrationTest")` branches in `Program.cs`).
- File naming: one `*Tests.cs` per scenario, not per source class.

## Conventions

- **No `// Arrange / // Act / // Assert` comments.** Let the structure speak.
- Use Shouldly (`result.ShouldBe(...)`, `collection.ShouldContain(...)`).
- Match nearby files for test method naming (`MethodOrBehavior_StateUnderTest_ExpectedResult`).
- Build test data inline with object initializers; do not introduce builders for one-off cases.

## Example

From `src/ume-app-estateservice/Umea.se.EstateService.Test/Search/InMemorySearchServiceAddressTests.cs`:

```csharp
[Fact]
public void Search_FindsDocumentsByAddressTokens()
{
    DateTimeOffset now = DateTimeOffset.UtcNow;

    PythagorasDocument building = new()
    {
        Id = 1,
        Type = NodeType.Building,
        Name = "Library",
        PopularName = "Central Library",
        Address = new AddressModel("Skolgatan 31A", "901 84", "Umeå", string.Empty, string.Empty),
        Ancestors = [],
        UpdatedAt = now,
        RankScore = 1
    };

    InMemorySearchService service = new([building, /* ... */]);
    // ... call service.Search(...) and assert with Shouldly
}
```
