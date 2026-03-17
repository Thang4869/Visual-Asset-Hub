namespace VAH.Backend.Models.ValueObjects;

using System;
using System.IO;
using global::VAH.Backend.Models;

/// <summary>
/// Immutable value object representing a validated file name.
/// </summary>
public readonly record struct FileName
{
    public string Value { get; }

    public FileName(string value)
    {
        Value = AssetValidator.ValidateFileName(value);
    }

    public static FileName Parse(string value) => new FileName(value);

    public override string ToString() => Value;
}
