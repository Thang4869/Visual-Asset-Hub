using System;
using System.Text.RegularExpressions;

namespace VAH.Backend.Models;

public sealed partial class StandardAssetValidator : IAssetValidator
{
    [GeneratedRegex(@"^#?([0-9A-Fa-f]{3}|[0-9A-Fa-f]{4}|[0-9A-Fa-f]{6}|[0-9A-Fa-f]{8})$")]
    private static partial Regex HexColorPattern();

    public bool IsValidHexColor(string colorCode) =>
        !string.IsNullOrWhiteSpace(colorCode) && HexColorPattern().IsMatch(colorCode.Trim());

    public string NormalizeHexColor(string colorCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(colorCode);

        var code = colorCode.Trim();

        if (!IsValidHexColor(code))
            throw new ArgumentException($"Invalid hex color code: '{colorCode}'.");

        return code.StartsWith('#') ? code : "#" + code;
    }

    public bool IsValidUrl(string url) =>
        !string.IsNullOrWhiteSpace(url)
        && Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri)
        && (uri.Scheme == "http" || uri.Scheme == "https");

    public string ValidateUrl(string url)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(url);

        var trimmed = url.Trim();

        if (!IsValidUrl(trimmed))
            throw new ArgumentException("Invalid URL format. Must be absolute http or https.");

        return trimmed;
    }

    public string ValidateFileName(string fileName, int maxLength = 500)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);

        var trimmed = fileName.Trim();

        if (trimmed.Length > maxLength)
            throw new ArgumentException($"File name exceeds maximum length of {maxLength} characters.");

        return trimmed;
    }
}
