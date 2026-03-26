# DOCUMENTATION STANDARDS — XML Doc, JSDoc & ADR Format

> **Last Updated**: 2026-03-26

---

## §1 — XML Documentation (.NET 9)

### Required Coverage

| Element | Tags Required | Severity |
|---------|--------------|----------|
| `public interface` | `<summary>`, `<remarks>` (Domain, Pattern, Implementations) | `[MUST]` |
| `public class` (service) | `<summary>`, `<remarks>` (Domain, Dependencies) | `[MUST]` |
| `public method` | `<summary>`, `<param>`, `<returns>`, `<exception>` | `[MUST]` |
| `public property` | `<summary>` | `[SHOULD]` |
| `enum` values | `<summary>` | `[SHOULD]` |

### Enable in `.csproj`
```xml
<PropertyGroup>
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
    <NoWarn>$(NoWarn);1591</NoWarn>
</PropertyGroup>
```

### Generate API Docs
```bash
dotnet tool install -g docfx
docfx init -q && docfx build
```

> Note: `VAH.Backend/VAH.Backend.csproj` currently does not include `GenerateDocumentationFile`. Enabling XML doc generation is optional; if you want build-produced XML docs for CI or docfx, add the above snippet to the project file or a Directory.Build.props. Keep `NoWarn` for missing XML comments (`1591`) if you do not wish to block builds.

## §2 — JSDoc (React 19)

### Required Coverage

| Element | Tags Required | Severity |
|---------|--------------|----------|
| API service class | `@class`, `@extends`, `@description` | `[MUST]` |
| API method | `@param`, `@returns`, `@throws` | `[MUST]` |
| Custom hook | `@hook`, `@description`, `@returns`, `@dependency` | `[MUST]` |
| Component | `@component`, `@description`, `@param` (props) | `[SHOULD]` |

### Generate
```json
{ "scripts": { "docs": "jsdoc src/ -r -d docs/generated" } }
```

## §3 — ADR (Architecture Decision Record) Format

```markdown
# ADR-NNN: Title

**Status**: Proposed | Accepted | Deprecated | Superseded by ADR-XXX  
**Date**: YYYY-MM-DD  
**Deciders**: Names

## Context
What is the issue that we're seeing that is motivating this decision?

## Decision
What is the change that we're proposing/doing?

## Consequences
### Positive
### Negative
### Neutral

## Alternatives Considered
| Option | Pros | Cons | Rejected Because |
```

## §4 — Markdown File Conventions

| Rule | Standard |
|------|----------|
| Headers | `#` → `##` → `###` (max 3 levels in body) |
| Section numbering | `§1`, `§2`, ... with subsections `1.1`, `1.2` |
| Code blocks | Triple backtick with language (`csharp`, `javascript`, `bash`) |
| Tables | For structured data, comparisons, rules |
| Diagrams | ASCII art (no external image dependencies) |
| Language | Mixed Vietnamese (explanations) + English (code terms, headers) |
| File naming | `UPPER_SNAKE_CASE.md` |

---

> **Document End**
