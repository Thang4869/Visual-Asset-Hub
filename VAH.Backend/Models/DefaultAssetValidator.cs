namespace VAH.Backend.Models;

/// <summary>
/// Default wrapper implementing <see cref="IAssetValidator"/> by delegating to the existing static <see cref="AssetValidator"/> helper.
/// This provides an injectable implementation without changing existing static APIs.
/// </summary>
public sealed class DefaultAssetValidator : IAssetValidator
{
    public static readonly DefaultAssetValidator Instance = new();

    private readonly IAssetValidator _impl;

    private DefaultAssetValidator()
    {
        _impl = new StandardAssetValidator();
    }

    public bool IsValidHexColor(string colorCode) => _impl.IsValidHexColor(colorCode);
    public string NormalizeHexColor(string colorCode) => _impl.NormalizeHexColor(colorCode);
    public bool IsValidUrl(string url) => _impl.IsValidUrl(url);
    public string ValidateUrl(string url) => _impl.ValidateUrl(url);
    public string ValidateFileName(string fileName, int maxLength = 500) => _impl.ValidateFileName(fileName, maxLength);
}
