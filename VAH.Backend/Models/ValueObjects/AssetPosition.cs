namespace VAH.Backend.Models.ValueObjects;

using System;
using System.Globalization;

/// <summary>
/// Immutable value object representing an asset canvas position.
/// </summary>
public readonly record struct AssetPosition
{
    /// <summary>
    /// X coordinate (canvas units).
    /// </summary>
    public double X { get; }

    /// <summary>
    /// Y coordinate (canvas units).
    /// </summary>
    public double Y { get; }

    /// <summary>
    /// Represents the origin (0,0).
    /// </summary>
    public static readonly AssetPosition Zero = new(0d, 0d);

    /// <summary>
    /// Creates a new <see cref="AssetPosition"/>.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when a coordinate is not a finite number.</exception>
    public AssetPosition(double x, double y)
    {
        if (double.IsNaN(x) || double.IsInfinity(x)) throw new ArgumentException("x must be a finite number.", nameof(x));
        if (double.IsNaN(y) || double.IsInfinity(y)) throw new ArgumentException("y must be a finite number.", nameof(y));

        X = x;
        Y = y;
    }

    /// <summary>
    /// Allows deconstruction: <c>var (x,y) = position;</c>
    /// </summary>
    public void Deconstruct(out double x, out double y) { x = X; y = Y; }

    /// <summary>
    /// Returns the position formatted using invariant culture.
    /// </summary>
    public override string ToString() => string.Format(CultureInfo.InvariantCulture, "({0}, {1})", X, Y);
}
