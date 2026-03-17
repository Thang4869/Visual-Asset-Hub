namespace VAH.Backend.Models;

/// <summary>
/// Default wrapper implementing <see cref="IAssetValidator"/> by delegating to the existing static <see cref="AssetValidator"/> helper.
/// This provides an injectable implementation without changing existing static APIs.
/// </summary>
public sealed class DefaultAssetValidator : IAssetValidator
{
    public static readonly DefaultAssetValidator Instance = new();

    private DefaultAssetValidator() { }

    public bool IsValidHexColor(string colorCode) => AssetValidator.IsValidHexColor(colorCode);
    public string NormalizeHexColor(string colorCode) => AssetValidator.NormalizeHexColor(colorCode);
    public bool IsValidUrl(string url) => AssetValidator.IsValidUrl(url);
    public string ValidateUrl(string url) => AssetValidator.ValidateUrl(url);
    public string ValidateFileName(string fileName, int maxLength = 500) => AssetValidator.ValidateFileName(fileName, maxLength);
}
