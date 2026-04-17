# Code Style

The authoritative source is the root `.editorconfig`. Combined with `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` in every `.csproj`, any rule with severity `warning` or `error` will fail the build.

## Language Targets

- C# language version: latest LTS (currently C# 12 features per `.github/copilot-instructions.md`).
- Target framework: `net10.0` (set per project — do not change `global.json`).
- Nullable reference types: enabled. Trust the annotations; only null-check at entry points.

## Formatting

- 4-space indentation in `.cs`; 2-space in `*.props`, `*.targets`, `*.csproj`, JSON, YAML.
- File-scoped namespaces (`csharp_style_namespace_declarations = file_scoped`, `IDE0161` warning).
- Mandatory braces, including for one-line blocks (`csharp_prefer_braces = true`, `IDE0011` warning).
- Newline before opening brace (`csharp_new_line_before_open_brace = all`), and before `else`/`catch`/`finally`.
- Max line length: 120. Wrap parameters/arguments with `chop_if_long`.
- No multiple blank lines (`IDE2000` warning).
- Sort `System.*` usings first (`dotnet_sort_system_directives_first`).
- Remove unused usings (`IDE0005` warning).

## `var` and Types

- Prefer **explicit types** everywhere. `csharp_style_var_for_built_in_types`, `csharp_style_var_when_type_is_apparent`, and `csharp_style_var_elsewhere` are all `false:warning` — they fail the build.
- Use predefined keyword aliases (`int`, `string`) over BCL names (`Int32`, `String`).
- No `this.` qualifier on fields or properties.

## Naming

- `private` / `internal` fields use `_camelCase` (e.g., `_docs`, `_idx`).
- `const` fields use `PascalCase`.
- Modifier order: `public, private, protected, internal, static, extern, new, virtual, abstract, sealed, override, readonly, unsafe, volatile, async`.

## Patterns

Preferred — explicit types, file-scoped namespace, sealed class, collection-expression init (from `Search/Indexing/Posting.cs`):

```csharp
namespace Umea.se.EstateService.Logic.Search.Indexing;

internal sealed class Posting(int docId, Field field, int position)
{
    public int DocId = docId;
    public Field Field = field;
    public List<int> Positions = [position];
}
```

- Prefer pattern matching and switch expressions.
- Use `nameof(...)` instead of string literals for member names.
- Forward `CancellationToken` to async methods that accept one (`CA2016` warning).
- Use `string.Contains(char)` over `string.Contains(string)` for single chars; use `StartsWith` over `IndexOf == 0`; prefer `IsEmpty` over `Count == 0`.

## Public API Documentation

- Add XML doc comments and Swagger annotations for any public Controller endpoint, with `<example>` and `<code>` where relevant. `CS1591` (missing XML doc) is silenced globally, so this is a team convention rather than build-enforced.

## Notable Diagnostics

- `CA2007` (`ConfigureAwait`): suggestion-level only, and `NoWarn` in `Umea.se.EstateService.API.csproj`. Don't add `.ConfigureAwait(false)` calls in API code.
- `CA1822` (make member static): silent for private members; suggestion otherwise.
- `IDE0036` enforces the modifier order above.
