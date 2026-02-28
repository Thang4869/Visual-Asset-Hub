using Microsoft.EntityFrameworkCore;
using VAH.Backend.Data;
using VAH.Backend.Models;

namespace VAH.Backend.Services;

public class TagService : ITagService
{
    private readonly AppDbContext _context;
    private readonly ILogger<TagService> _logger;

    public TagService(AppDbContext context, ILogger<TagService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<Tag>> GetAllAsync(string userId, CancellationToken ct = default)
    {
        return await _context.Tags
            .Where(t => t.UserId == userId)
            .OrderBy(t => t.Name)
            .ToListAsync(ct);
    }

    public async Task<Tag> GetByIdAsync(int id, string userId, CancellationToken ct = default)
    {
        return await _context.Tags
            .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId, ct)
            ?? throw new KeyNotFoundException($"Tag {id} not found.");
    }

    public async Task<Tag> CreateAsync(CreateTagDto dto, string userId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            throw new ArgumentException("Tag name is required.");

        var normalized = dto.Name.Trim().ToLowerInvariant();

        // Check for duplicate
        var existing = await _context.Tags
            .FirstOrDefaultAsync(t => t.NormalizedName == normalized && t.UserId == userId, ct);
        if (existing != null)
            return existing; // Return existing instead of error

        var tag = new Tag
        {
            Color = dto.Color,
            UserId = userId,
            CreatedAt = DateTime.UtcNow
        };
        tag.SetName(dto.Name);

        _context.Tags.Add(tag);
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Tag created: {Name} (Id={Id}) by user {UserId}", tag.Name, tag.Id, userId);
        return tag;
    }

    public async Task<Tag> UpdateAsync(int id, UpdateTagDto dto, string userId, CancellationToken ct = default)
    {
        var tag = await _context.Tags
            .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId, ct)
            ?? throw new KeyNotFoundException($"Tag {id} not found.");

        tag.UpdateFrom(dto);

        await _context.SaveChangesAsync(ct);
        return tag;
    }

    public async Task<bool> DeleteAsync(int id, string userId, CancellationToken ct = default)
    {
        var tag = await _context.Tags
            .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId, ct)
            ?? throw new KeyNotFoundException($"Tag {id} not found.");

        _context.Tags.Remove(tag);
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Tag deleted: {Name} (Id={Id}) by user {UserId}", tag.Name, id, userId);
        return true;
    }

    /// <summary>
    /// Get or create tags by name. Returns existing tags if they already exist.
    /// </summary>
    public async Task<List<Tag>> GetOrCreateTagsAsync(IEnumerable<string> tagNames, string userId, CancellationToken ct = default)
    {
        var result = new List<Tag>();
        var namesToCreate = new List<string>();

        foreach (var name in tagNames.Where(n => !string.IsNullOrWhiteSpace(n)).Distinct())
        {
            ct.ThrowIfCancellationRequested();
            var normalized = name.Trim().ToLowerInvariant();
            var existing = await _context.Tags
                .FirstOrDefaultAsync(t => t.NormalizedName == normalized && t.UserId == userId, ct);

            if (existing != null)
            {
                result.Add(existing);
            }
            else
            {
                namesToCreate.Add(name.Trim());
            }
        }

        foreach (var name in namesToCreate)
        {
            var tag = new Tag
            {
                Name = name,
                NormalizedName = name.ToLowerInvariant(),
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            };
            _context.Tags.Add(tag);
            result.Add(tag);
        }

        if (namesToCreate.Count > 0)
            await _context.SaveChangesAsync(ct);

        return result;
    }

    /// <summary>Replace all tags on an asset.</summary>
    public async Task SetAssetTagsAsync(int assetId, List<int> tagIds, string userId, CancellationToken ct = default)
    {
        var asset = await _context.Assets
            .Include(a => a.AssetTags)
            .FirstOrDefaultAsync(a => a.Id == assetId && a.UserId == userId, ct)
            ?? throw new KeyNotFoundException("Asset not found.");

        // Remove all existing
        _context.AssetTags.RemoveRange(asset.AssetTags);

        // Add new
        foreach (var tagId in tagIds.Distinct())
        {
            ct.ThrowIfCancellationRequested();
            var tagExists = await _context.Tags.AnyAsync(t => t.Id == tagId && t.UserId == userId, ct);
            if (tagExists)
            {
                _context.AssetTags.Add(new AssetTag { AssetId = assetId, TagId = tagId });
            }
        }

        // Also update the legacy comma-separated Tags field for backward compatibility
        var tagNames = await _context.Tags
            .Where(t => tagIds.Contains(t.Id) && t.UserId == userId)
            .Select(t => t.Name)
            .ToListAsync(ct);
        asset.Tags = string.Join(",", tagNames);

        await _context.SaveChangesAsync(ct);
    }

    /// <summary>Add tags to an asset (additive).</summary>
    public async Task AddAssetTagsAsync(int assetId, List<int> tagIds, string userId, CancellationToken ct = default)
    {
        var asset = await _context.Assets
            .Include(a => a.AssetTags)
            .FirstOrDefaultAsync(a => a.Id == assetId && a.UserId == userId, ct)
            ?? throw new KeyNotFoundException("Asset not found.");

        var existingTagIds = asset.AssetTags.Select(at => at.TagId).ToHashSet();

        foreach (var tagId in tagIds.Distinct().Where(id => !existingTagIds.Contains(id)))
        {
            ct.ThrowIfCancellationRequested();
            var tagExists = await _context.Tags.AnyAsync(t => t.Id == tagId && t.UserId == userId, ct);
            if (tagExists)
            {
                _context.AssetTags.Add(new AssetTag { AssetId = assetId, TagId = tagId });
            }
        }

        await _context.SaveChangesAsync(ct);

        // Sync legacy Tags field
        await SyncLegacyTagsFieldAsync(assetId, userId, ct);
    }

    /// <summary>Remove tags from an asset.</summary>
    public async Task RemoveAssetTagsAsync(int assetId, List<int> tagIds, string userId, CancellationToken ct = default)
    {
        var junctions = await _context.AssetTags
            .Where(at => at.AssetId == assetId && tagIds.Contains(at.TagId))
            .ToListAsync(ct);

        // Verify asset ownership
        var assetOwned = await _context.Assets.AnyAsync(a => a.Id == assetId && a.UserId == userId, ct);
        if (!assetOwned) throw new KeyNotFoundException("Asset not found.");

        _context.AssetTags.RemoveRange(junctions);
        await _context.SaveChangesAsync(ct);

        await SyncLegacyTagsFieldAsync(assetId, userId, ct);
    }

    public async Task<List<Tag>> GetAssetTagsAsync(int assetId, string userId, CancellationToken ct = default)
    {
        var assetOwned = await _context.Assets.AnyAsync(a => a.Id == assetId && a.UserId == userId, ct);
        if (!assetOwned) throw new KeyNotFoundException("Asset not found.");

        return await _context.AssetTags
            .Where(at => at.AssetId == assetId)
            .Include(at => at.Tag)
            .Select(at => at.Tag!)
            .OrderBy(t => t.Name)
            .ToListAsync(ct);
    }

    /// <summary>
    /// Migrate existing comma-separated Tags field to the new many-to-many system.
    /// </summary>
    public async Task MigrateCommaSeparatedTagsAsync(string userId, CancellationToken ct = default)
    {
        var assets = await _context.Assets
            .Where(a => a.UserId == userId && !string.IsNullOrEmpty(a.Tags))
            .ToListAsync(ct);

        var allTagNames = assets
            .SelectMany(a => a.Tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (allTagNames.Count == 0) return;

        // Create all tags
        var tags = await GetOrCreateTagsAsync(allTagNames, userId, ct);
        var tagMap = tags.ToDictionary(t => t.NormalizedName, t => t.Id);

        // Link assets to tags
        foreach (var asset in assets)
        {
            ct.ThrowIfCancellationRequested();
            var names = asset.Tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var name in names)
            {
                var normalized = name.ToLowerInvariant();
                if (tagMap.TryGetValue(normalized, out var tagId))
                {
                    var exists = await _context.AssetTags
                        .AnyAsync(at => at.AssetId == asset.Id && at.TagId == tagId, ct);
                    if (!exists)
                    {
                        _context.AssetTags.Add(new AssetTag { AssetId = asset.Id, TagId = tagId });
                    }
                }
            }
        }

        await _context.SaveChangesAsync(ct);
        _logger.LogInformation("Migrated {Count} comma-separated tags for user {UserId}", allTagNames.Count, userId);
    }

    private async Task SyncLegacyTagsFieldAsync(int assetId, string userId, CancellationToken ct)
    {
        var asset = await _context.Assets.FindAsync([assetId], ct);
        if (asset == null) return;

        var tagNames = await _context.AssetTags
            .Where(at => at.AssetId == assetId)
            .Include(at => at.Tag)
            .Select(at => at.Tag!.Name)
            .ToListAsync(ct);

        asset.Tags = string.Join(",", tagNames);
        await _context.SaveChangesAsync(ct);
    }
}
