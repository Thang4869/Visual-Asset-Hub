namespace VAH.Backend.Models;

/// <summary>
/// Content type of an asset. Determines behavior and display.
/// Stored as string in DB for backward compatibility.
/// </summary>
public enum AssetContentType
{
    Image,
    Link,
    Color,
    ColorGroup,
    Folder,
    File
}

/// <summary>
/// Collection type. Determines which asset types are allowed.
/// </summary>
public enum CollectionType
{
    Default,
    Image,
    Link,
    Color
}

/// <summary>
/// Layout display mode for a collection.
/// </summary>
public enum LayoutType
{
    Grid,
    List,
    Canvas
}

/// <summary>
/// Extension methods and mapping helpers for enums ↔ DB string conversion.
/// Keeps DB values lowercase for backward compatibility with existing data.
/// </summary>
public static class EnumMappings
{
    // ──── AssetContentType ────

    private static readonly Dictionary<string, AssetContentType> ContentTypeFromString = new(StringComparer.OrdinalIgnoreCase)
    {
        ["image"] = AssetContentType.Image,
        ["link"] = AssetContentType.Link,
        ["color"] = AssetContentType.Color,
        ["color-group"] = AssetContentType.ColorGroup,
        ["folder"] = AssetContentType.Folder,
        ["file"] = AssetContentType.File,
    };

    private static readonly Dictionary<AssetContentType, string> ContentTypeToString = new()
    {
        [AssetContentType.Image] = "image",
        [AssetContentType.Link] = "link",
        [AssetContentType.Color] = "color",
        [AssetContentType.ColorGroup] = "color-group",
        [AssetContentType.Folder] = "folder",
        [AssetContentType.File] = "file",
    };

    public static AssetContentType ToAssetContentType(this string value) =>
        ContentTypeFromString.TryGetValue(value, out var result) ? result : AssetContentType.File;

    public static string ToDbString(this AssetContentType value) =>
        ContentTypeToString.TryGetValue(value, out var result) ? result : "file";

    // ──── CollectionType ────

    private static readonly Dictionary<string, CollectionType> CollectionTypeFromString = new(StringComparer.OrdinalIgnoreCase)
    {
        ["default"] = CollectionType.Default,
        ["image"] = CollectionType.Image,
        ["link"] = CollectionType.Link,
        ["color"] = CollectionType.Color,
    };

    private static readonly Dictionary<CollectionType, string> CollectionTypeToString = new()
    {
        [CollectionType.Default] = "default",
        [CollectionType.Image] = "image",
        [CollectionType.Link] = "link",
        [CollectionType.Color] = "color",
    };

    public static CollectionType ToCollectionType(this string value) =>
        CollectionTypeFromString.TryGetValue(value, out var result) ? result : CollectionType.Default;

    public static string ToDbString(this CollectionType value) =>
        CollectionTypeToString.TryGetValue(value, out var result) ? result : "default";

    // ──── LayoutType ────

    private static readonly Dictionary<string, LayoutType> LayoutTypeFromString = new(StringComparer.OrdinalIgnoreCase)
    {
        ["grid"] = LayoutType.Grid,
        ["list"] = LayoutType.List,
        ["canvas"] = LayoutType.Canvas,
    };

    private static readonly Dictionary<LayoutType, string> LayoutTypeToString = new()
    {
        [LayoutType.Grid] = "grid",
        [LayoutType.List] = "list",
        [LayoutType.Canvas] = "canvas",
    };

    public static LayoutType ToLayoutType(this string value) =>
        LayoutTypeFromString.TryGetValue(value, out var result) ? result : LayoutType.Grid;

    public static string ToDbString(this LayoutType value) =>
        LayoutTypeToString.TryGetValue(value, out var result) ? result : "grid";
}
