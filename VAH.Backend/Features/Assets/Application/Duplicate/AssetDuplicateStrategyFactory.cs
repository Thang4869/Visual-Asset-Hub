using System;
using System.Collections.Generic;
using System.Linq;

namespace VAH.Backend.Features.Assets.Application.Duplicate;

internal sealed class AssetDuplicateStrategyFactory : IAssetDuplicateStrategyFactory
{
    private readonly IReadOnlyCollection<IAssetDuplicateStrategy> _strategies;

    public AssetDuplicateStrategyFactory(IEnumerable<IAssetDuplicateStrategy> strategies)
    {
        _strategies = strategies?.ToArray()
            ?? throw new ArgumentNullException(nameof(strategies));
    }

    public IAssetDuplicateStrategy Create(int? targetFolderId)
        => _strategies.FirstOrDefault(strategy => strategy.CanHandle(targetFolderId))
           ?? throw new InvalidOperationException($"No duplicate strategy found for targetFolderId='{targetFolderId}'.");
}
