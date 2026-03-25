namespace VAH.Backend.Models.ValueObjects;

using System;
using global::VAH.Backend.Models;

/// <summary>
/// Immutable value object representing a normalized hex color code (e.g. #FF5733).
/// Use <see cref="Parse(string)"/> to construct (throws on invalid input).
/// </summary>
public readonly record struct ColorCode
{
    /// <summary>
    /// Normalized hex color code (e.g. #FF5733). Guaranteed to be non-null for a constructed instance.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Creates a new <see cref="ColorCode"/> from an input value and normalizes it.
    /// </summary>
    /// <exception cref="ArgumentException">If the provided value cannot be normalized to a valid hex code.</exception>
    public ColorCode(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("value must not be null or whitespace.", nameof(value));

        Value = DefaultAssetValidator.Instance.NormalizeHexColor(value);
        if (string.IsNullOrWhiteSpace(Value)) throw new ArgumentException("Normalized color code is invalid.", nameof(value));
    }

    /// <summary>
    /// Parses and returns a <see cref="ColorCode"/>. Throws on invalid input.
    /// </summary>
    public static ColorCode Parse(string value) => new ColorCode(value);

    /// <summary>
    /// Attempts to parse/normalize the provided string into a <see cref="ColorCode"/>.
    /// </summary>
    public static bool TryParse(string? value, out ColorCode result)
    {
        result = default;
        if (string.IsNullOrWhiteSpace(value)) return false;
        try
        {
            result = new ColorCode(value!);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Returns the normalized color string.
    /// </summary>
    public override string ToString() => Value;
}
