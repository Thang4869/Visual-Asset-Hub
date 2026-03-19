namespace VAH.Backend.Models.ValueObjects;

using System;
using global::VAH.Backend.Models;

/// <summary>
/// Immutable value object representing a normalized hex color code (e.g. #FF5733).
/// Use <see cref="Parse(string)"/> to construct (throws on invalid input).
/// </summary>
public readonly record struct ColorCode
{
    public string Value { get; }

    public ColorCode(string value)
    {
        Value = DefaultAssetValidator.Instance.NormalizeHexColor(value);
    }

    public static ColorCode Parse(string value) => new ColorCode(value);

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

    public override string ToString() => Value;
}
