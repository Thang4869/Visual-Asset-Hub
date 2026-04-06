using VAH.Backend.Services;
using VAH.Backend.Models;
using VAH.Backend.Data;
using VAH.Backend.Tests.Fixtures;
using Microsoft.Extensions.Logging;

namespace VAH.Backend.Tests.Unit;

public class AssetServiceTests : TestFixture
{
    private readonly Mock<IStorageService> _mockStorage;
    private readonly Mock<IThumbnailService> _mockThumbnail;
    private readonly Mock<INotificationService> _mockNotifier;
    private readonly Mock<IPermissionService> _mockPermissions;
    private readonly Mock<IAssetValidator> _mockValidator;
    private readonly Mock<IAssetFactory> _mockFactory;
    private readonly Mock<IAssetMapper> _mockMapper;
    private readonly Mock<ILogger<AssetService>> _mockLogger;
    private readonly FileUploadConfig _uploadConfig;
    private readonly AssetCleanupHelper _cleanup;

    public AssetServiceTests()
    {
        _mockStorage = new Mock<IStorageService>();
        _mockThumbnail = new Mock<IThumbnailService>();
        _mockNotifier = new Mock<INotificationService>();
        _mockPermissions = new Mock<IPermissionService>();
        _mockValidator = new Mock<IAssetValidator>();
        _mockFactory = new Mock<IAssetFactory>();
        _mockMapper = new Mock<IAssetMapper>();
        _mockLogger = new Mock<ILogger<AssetService>>();
        
        _uploadConfig = new FileUploadConfig { MaxFileSizeMB = 50 };
        _cleanup = new AssetCleanupHelper(DbContext, _mockStorage.Object, _mockLogger.Object);
    }

    private AssetService CreateAssetService()
    {
        return new AssetService(
            DbContext,
            _mockStorage.Object,
            _uploadConfig,
            _mockThumbnail.Object,
            _mockNotifier.Object,
            _cleanup,
            _mockLogger.Object,
            _mockPermissions.Object,
            _mockValidator.Object,
            _mockFactory.Object,
            _mockMapper.Object
        );
    }

    [Fact]
    public async Task GetByIdAsync_WithValidId_ReturnsAsset()
    {
        // Arrange
        var userId = "test-user";
        var asset = new AssetBuilder()
            .WithId(1)
            .WithName("Test Asset")
            .WithUserId(userId)
            .AsPublic()
            .Build();

        await DbContext.Assets.AddAsync(asset);
        await DbContext.SaveChangesAsync();

        var expectedDto = new AssetResponseDto 
        { 
            Id = 1, 
            Name = "Test Asset", 
            UserId = userId 
        };

        _mockMapper.Setup(m => m.ToResponseDto(It.IsAny<Asset>()))
            .Returns(expectedDto);

        _mockPermissions.Setup(p => p.HasAssetAccessAsync(1, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var service = CreateAssetService();

        // Act
        var result = await service.GetByIdAsync(1, userId);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(1);
        result.Name.Should().Be("Test Asset");
        result.UserId.Should().Be(userId);
        
        _mockPermissions.Verify(p => p.HasAssetAccessAsync(1, userId, It.IsAny<CancellationToken>()), Times.Once);
        _mockMapper.Verify(m => m.ToResponseDto(It.IsAny<Asset>()), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_WithInvalidId_ThrowsNotFoundException()
    {
        // Arrange
        var userId = "test-user";
        var service = CreateAssetService();

        _mockPermissions.Setup(p => p.HasAssetAccessAsync(999, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act & Assert
        await FluentActions.Invoking(() => service.GetByIdAsync(999, userId))
            .Should().ThrowAsync<NotFoundException>()
            .WithMessage("*Asset*999*");
    }

    [Fact]
    public async Task CreateColorAsync_WithValidData_ReturnsColorAsset()
    {
        // Arrange
        var userId = "test-user";
        var createDto = new CreateColorDto
        {
            ColorCode = "#FF0000",
            Name = "Red Color",
            CollectionId = 1
        };

        var colorAsset = new ColorAsset
        {
            Id = 1,
            Name = "Red Color",
            ColorCode = "#FF0000",
            UserId = userId,
            CollectionId = 1
        };

        var expectedDto = new AssetResponseDto
        {
            Id = 1,
            Name = "Red Color",
            AssetType = AssetType.Color,
            UserId = userId
        };

        _mockValidator.Setup(v => v.ValidateCreateColorAsync(createDto, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockFactory.Setup(f => f.CreateColorAssetAsync(createDto, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(colorAsset);

        _mockMapper.Setup(m => m.ToResponseDto(It.IsAny<Asset>()))
            .Returns(expectedDto);

        var service = CreateAssetService();

        // Act
        var result = await service.CreateColorAsync(createDto, userId);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(1);
        result.Name.Should().Be("Red Color");
        result.AssetType.Should().Be(AssetType.Color);
        
        _mockValidator.Verify(v => v.ValidateCreateColorAsync(createDto, It.IsAny<CancellationToken>()), Times.Once);
        _mockFactory.Verify(f => f.CreateColorAssetAsync(createDto, userId, It.IsAny<CancellationToken>()), Times.Once);
        _mockNotifier.Verify(n => n.NotifyAssetCreatedAsync(It.IsAny<Asset>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdatePositionAsync_WithValidData_UpdatesPosition()
    {
        // Arrange
        var userId = "test-user";
        var asset = new AssetBuilder()
            .WithId(1)
            .WithName("Test Asset")
            .WithUserId(userId)
            .WithPosition(0, 0)
            .Build();

        await DbContext.Assets.AddAsync(asset);
        await DbContext.SaveChangesAsync();

        var expectedDto = new AssetResponseDto
        {
            Id = 1,
            Name = "Test Asset",
            PositionX = 100.5,
            PositionY = 200.5
        };

        _mockPermissions.Setup(p => p.HasAssetEditAccessAsync(1, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _mockMapper.Setup(m => m.ToResponseDto(It.IsAny<Asset>()))
            .Returns(expectedDto);

        var service = CreateAssetService();

        // Act
        var result = await service.UpdatePositionAsync(1, 100.5, 200.5, userId);

        // Assert
        result.Should().NotBeNull();
        result.PositionX.Should().Be(100.5);
        result.PositionY.Should().Be(200.5);

        // Verify database was updated
        var updatedAsset = await DbContext.Assets.FindAsync(1);
        updatedAsset.Should().NotBeNull();
        updatedAsset!.PositionX.Should().Be(100.5);
        updatedAsset.PositionY.Should().Be(200.5);
        
        _mockPermissions.Verify(p => p.HasAssetEditAccessAsync(1, userId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAssetAsync_WithValidId_DeletesAssetAndCleanup()
    {
        // Arrange
        var userId = "test-user";
        var asset = new AssetBuilder()
            .WithId(1)
            .WithName("Test Asset")
            .WithUserId(userId)
            .WithFilePath("/uploads/test.jpg")
            .Build();

        await DbContext.Assets.AddAsync(asset);
        await DbContext.SaveChangesAsync();

        _mockPermissions.Setup(p => p.HasAssetDeleteAccessAsync(1, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var service = CreateAssetService();

        // Act
        var result = await service.DeleteAssetAsync(1, userId);

        // Assert
        result.Should().BeTrue();

        // Verify asset is deleted from database
        var deletedAsset = await DbContext.Assets.FindAsync(1);
        deletedAsset.Should().BeNull();
        
        _mockPermissions.Verify(p => p.HasAssetDeleteAccessAsync(1, userId, It.IsAny<CancellationToken>()), Times.Once);
        _mockNotifier.Verify(n => n.NotifyAssetDeletedAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}