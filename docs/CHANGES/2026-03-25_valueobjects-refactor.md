---
title: ValueObjects refactor
date: 2026-03-25
---

## Refactored — ValueObjects Hardening (2026-03-25)

Purpose
- Type: Refactor
- Rationale: Improve safety, developer ergonomics and consistency for small domain value objects used across the codebase. The changes add explicit validation, clearer error messages, parsing helpers and small API ergonomics (Deconstruct/Zero) to reduce bugs and make unit testing easier.

Scope
- `Models/ValueObjects/AssetPosition.cs`
- `Models/ValueObjects/FileName.cs`
- `Models/ValueObjects/ColorCode.cs`

Before & After

AssetPosition
- Before: constructor accepted two doubles and threw non-descriptive ArgumentException when NaN/Infinity; `ToString()` used default formatting.

```csharp
// BEFORE
public AssetPosition(double x, double y)
{
    if (double.IsNaN(x) || double.IsInfinity(x)) throw new ArgumentException("X must be a finite number.");
    if (double.IsNaN(y) || double.IsInfinity(y)) throw new ArgumentException("Y must be a finite number.");
    X = x; Y = y;
}
```

- After: parameter names passed to exceptions, `Zero` constant, `Deconstruct` added, and `ToString()` uses `CultureInfo.InvariantCulture` for deterministic formatting.

```csharp
// AFTER
public AssetPosition(double x, double y)
{
    if (double.IsNaN(x) || double.IsInfinity(x)) throw new ArgumentException("x must be a finite number.", nameof(x));
    if (double.IsNaN(y) || double.IsInfinity(y)) throw new ArgumentException("y must be a finite number.", nameof(y));
    X = x; Y = y;
}
public static readonly AssetPosition Zero = new(0d, 0d);
public void Deconstruct(out double x, out double y) { x = X; y = Y; }
```

FileName
- Before: constructor delegated to `DefaultAssetValidator.Instance.ValidateFileName(value)` without a null check; no TryParse; could silently accept empty/invalid results from validator.

```csharp
// BEFORE
public FileName(string value)
{
    Value = DefaultAssetValidator.Instance.ValidateFileName(value);
}
```

- After: explicit null-check, throws `ArgumentNullException` when input is null, throws `ArgumentException` if validation returns empty/invalid, and `TryParse` helper added for callers preferring non-throwing patterns.

```csharp
// AFTER
public FileName(string value)
{
    if (value is null) throw new ArgumentNullException(nameof(value));
    Value = DefaultAssetValidator.Instance.ValidateFileName(value);
    if (string.IsNullOrWhiteSpace(Value)) throw new ArgumentException("Validated file name is empty or invalid.", nameof(value));
}
public static bool TryParse(string? value, out FileName result) { ... }
```

ColorCode
- Before: constructor called `NormalizeHexColor` without null/whitespace guard; `TryParse` existed but normalization could yield empty string silently.

```csharp
// BEFORE
public ColorCode(string value)
{
    Value = DefaultAssetValidator.Instance.NormalizeHexColor(value);
}
```

- After: added argument guard (null/whitespace) and explicit `ArgumentException` when normalization fails; preserves `TryParse` API.

```csharp
// AFTER
public ColorCode(string value)
{
    if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("value must not be null or whitespace.", nameof(value));
    Value = DefaultAssetValidator.Instance.NormalizeHexColor(value);
    if (string.IsNullOrWhiteSpace(Value)) throw new ArgumentException("Normalized color code is invalid.", nameof(value));
}
```

Breaking changes / Notes for the team
- No database schema changes.
- No API controller contract changes.
- Callers relying on `FileName(string)` silently accepting `null` or validator-empty results must be updated: constructor now throws `ArgumentNullException` or `ArgumentException`. Prefer `FileName.TryParse(...)` where non-throwing behaviour is desired.
- `AssetPosition.ToString()` is now invariant-culture formatted — tests asserting locale-sensitive output must be adjusted.

Testing & Migration guidance
- Add unit tests for new guard clauses (`null`/whitespace/NaN/Infinity) and for `TryParse` success/failure paths.
- Update any internal helper code that previously assumed `FileName` could be constructed with null/empty input.

Author: Automated documentation update (Senior Engineer + Tech Writer)
