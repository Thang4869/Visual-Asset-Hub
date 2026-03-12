using System.Text.RegularExpressions;

namespace VAH.Backend.Models;

/// <summary>
/// Centralizes asset domain validation rules.
/// Called by AssetFactory (pre-construction) and Service layer.
/// Keeps validation logic DRY and testable in isolation.
/// </summary>
public static partial class AssetValidator
{
    // ── Color hex code ──

    [GeneratedRegex(@"^#?([0-9A-Fa-f]{3}|[0-9A-Fa-f]{4}|[0-9A-Fa-f]{6}|[0-9A-Fa-f]{8})$")]
    private static partial Regex HexColorPattern();

    /// <summary>Validate that the color code is a recognized hex format (3/4/6/8 digits, optional #).</summary>
    public static bool IsValidHexColor(string colorCode) =>
        !string.IsNullOrWhiteSpace(colorCode) && HexColorPattern().IsMatch(colorCode.Trim());

    /// <summary>
    /// Normalize a hex color code: trim, auto-prepend # if valid bare hex.
    /// Returns the normalized code or throws <see cref="ArgumentException"/>.
    /// </summary>
    public static string NormalizeHexColor(string colorCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(colorCode);

        var code = colorCode.Trim();

        if (!IsValidHexColor(code))
            throw new ArgumentException($"Invalid hex color code: '{colorCode}'.");

        return code.StartsWith('#') ? code : "#" + code;
    }

    // ── URL ──

    /// <summary>Validate that the URL is a well-formed absolute http(s) URI.</summary>
    public static bool IsValidUrl(string url) =>
        !string.IsNullOrWhiteSpace(url)
        && Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri)
        && (uri.Scheme == "http" || uri.Scheme == "https");

    /// <summary>
    /// Validate and return normalized URL. Throws <see cref="ArgumentException"/> on failure.
    /// </summary>
    public static string ValidateUrl(string url)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(url);

        var trimmed = url.Trim();

        if (!IsValidUrl(trimmed))
            throw new ArgumentException("Invalid URL format. Must be absolute http or https.");

        return trimmed;
    }

    // ── File name ──

    /// <summary>Validate that a file name is non-empty and within max length.</summary>
    public static string ValidateFileName(string fileName, int maxLength = 500)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);

        var trimmed = fileName.Trim();

        if (trimmed.Length > maxLength)
            throw new ArgumentException($"File name exceeds maximum length of {maxLength} characters.");

        return trimmed;
    }
}
