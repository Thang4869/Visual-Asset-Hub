namespace VAH.Backend.Features.Assets.Application.Duplicate;

internal interface IAssetDuplicateStrategyFactory
{
    IAssetDuplicateStrategy Create(int? targetFolderId);
}
