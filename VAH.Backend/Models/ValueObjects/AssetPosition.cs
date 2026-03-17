namespace VAH.Backend.Models.ValueObjects;

using System;

/// <summary>
/// Immutable value object representing an asset canvas position.
/// </summary>
public readonly record struct AssetPosition
{
    public double X { get; }
    public double Y { get; }

    public AssetPosition(double x, double y)
    {
        if (double.IsNaN(x) || double.IsInfinity(x)) throw new ArgumentException("X must be a finite number.");
        if (double.IsNaN(y) || double.IsInfinity(y)) throw new ArgumentException("Y must be a finite number.");

        X = x;
        Y = y;
    }

    public override string ToString() => $"({X}, {Y})";
}
