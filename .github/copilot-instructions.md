# Copilot Instructions

The full project guidance lives in [`AGENTS.md`](../AGENTS.md) and the
[`.ai/`](../.ai/) directory:

- [`.ai/code-style.md`](../.ai/code-style.md) — formatting, naming, `var` rules, notable analyzers
- [`.ai/testing.md`](../.ai/testing.md) — xUnit v3 + Shouldly conventions
- [`.ai/architecture.md`](../.ai/architecture.md) — modules and domain terms
- [`.ai/boundaries.md`](../.ai/boundaries.md) — always / ask first / never

## Copilot-specific nudges

- Make only high-confidence suggestions when reviewing code changes.
- Treat the `.editorconfig` and `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` as load-bearing — any rule at `warning` severity fails the build.
- Add XML doc + Swagger annotations to new public Controller endpoints (`<example>` / `<code>` where useful). `CS1591` is silenced globally, so this is a team convention, not a build failure.
- Never modify `global.json`, top-level `package.json`, or `NuGet.Config` files unless explicitly asked.
