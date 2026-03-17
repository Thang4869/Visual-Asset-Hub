namespace VAH.Backend.Models;

public interface IAssetValidator
{
    bool IsValidHexColor(string colorCode);
    string NormalizeHexColor(string colorCode);
    bool IsValidUrl(string url);
    string ValidateUrl(string url);
    string ValidateFileName(string fileName, int maxLength = 500);
}
