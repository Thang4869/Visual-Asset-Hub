namespace VAH.Backend.Models.ValueObjects;

using System;
using System.IO;
using global::VAH.Backend.Models;

/// <summary>
/// Immutable value object representing a validated file name.
/// </summary>
public readonly record struct FileName
{
    /// <summary>
    /// The validated file name. Never null or empty for a constructed instance.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Creates a new <see cref="FileName"/> after validating input via <see cref="DefaultAssetValidator"/>.
    /// </summary>
    /// <exception cref="ArgumentNullException">If <paramref name="value"/> is null.</exception>
    /// <exception cref="ArgumentException">If the value is invalid per validator.</exception>
    public FileName(string value)
    {
        if (value is null) throw new ArgumentNullException(nameof(value));

        Value = DefaultAssetValidator.Instance.ValidateFileName(value);
        if (string.IsNullOrWhiteSpace(Value)) throw new ArgumentException("Validated file name is empty or invalid.", nameof(value));
    }

    /// <summary>
    /// Parses and returns a <see cref="FileName"/>. Throws on invalid input.
    /// </summary>
    public static FileName Parse(string value) => new FileName(value);

    /// <summary>
    /// Attempts to parse a file name. Returns false on invalid input.
    /// </summary>
    public static bool TryParse(string? value, out FileName result)
    {
        result = default;
        if (string.IsNullOrWhiteSpace(value)) return false;
        try
        {
            result = new FileName(value);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Returns the validated file name string.
    /// </summary>
    public override string ToString() => Value;
}
