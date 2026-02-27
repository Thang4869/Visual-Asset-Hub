namespace VAH.Backend.Models;

// ──────────────────────────────────────────────────────────────
//  TPH (Table-Per-Hierarchy) subclasses of Asset.
//  EF Core materializes the correct subtype based on the
//  ContentType discriminator column (existing column, no migration).
// ──────────────────────────────────────────────────────────────

/// <summary>Image asset — uploaded image file with thumbnail support.</summary>
public class ImageAsset : Asset
{
    public override bool HasPhysicalFile => true;
    public override bool CanHaveThumbnails => true;
}

/// <summary>Link/bookmark asset — stores a URL reference.</summary>
public class LinkAsset : Asset
{
    public override bool HasPhysicalFile => false;
}

/// <summary>Color swatch asset — stores a hex color code.</summary>
public class ColorAsset : Asset
{
    public override bool HasPhysicalFile => false;
}

/// <summary>Color group — organizes color swatches together.</summary>
public class ColorGroupAsset : Asset
{
    public override bool HasPhysicalFile => false;
}

/// <summary>Folder — organizes assets hierarchically within a collection.</summary>
public class FolderAsset : Asset
{
    public override bool HasPhysicalFile => false;
}
