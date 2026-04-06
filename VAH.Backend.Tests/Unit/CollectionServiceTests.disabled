using VAH.Backend.Services;
using VAH.Backend.Models;
using VAH.Backend.Tests.Fixtures;
using Microsoft.Extensions.Logging;

namespace VAH.Backend.Tests.Unit;

public class CollectionServiceTests : TestFixture
{
    private readonly Mock<IPermissionService> _mockPermissions;
    private readonly Mock<INotificationService> _mockNotifier;
    private readonly Mock<ILogger<CollectionService>> _mockLogger;

    public CollectionServiceTests()
    {
        _mockPermissions = new Mock<IPermissionService>();
        _mockNotifier = new Mock<INotificationService>();
        _mockLogger = new Mock<ILogger<CollectionService>>();
    }

    private CollectionService CreateCollectionService()
    {
        return new CollectionService(
            DbContext,
            _mockPermissions.Object,
            _mockNotifier.Object,
            _mockLogger.Object
        );
    }

    [Fact]
    public async Task GetAllAsync_WithUserCollections_ReturnsUserCollections()
    {
        // Arrange
        var userId = "test-user";
        var otherUserId = "other-user";
        
        var userCollections = new[]
        {
            new CollectionBuilder().WithId(1).WithName("User Collection 1").WithUserId(userId).Build(),
            new CollectionBuilder().WithId(2).WithName("User Collection 2").WithUserId(userId).Build(),
        };

        var otherUserCollection = new CollectionBuilder()
            .WithId(3)
            .WithName("Other Collection")
            .WithUserId(otherUserId)
            .Build();

        await DbContext.Collections.AddRangeAsync(userCollections);
        await DbContext.Collections.AddAsync(otherUserCollection);
        await DbContext.SaveChangesAsync();

        var service = CreateCollectionService();

        // Act
        var result = await service.GetAllAsync(userId);

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(c => c.UserId == userId);
        result.Should().Contain(c => c.Name == "User Collection 1");
        result.Should().Contain(c => c.Name == "User Collection 2");
        result.Should().NotContain(c => c.Name == "Other Collection");
    }

    [Fact]
    public async Task GetByIdAsync_WithValidId_ReturnsCollection()
    {
        // Arrange
        var userId = "test-user";
        var collection = new CollectionBuilder()
            .WithId(1)
            .WithName("Test Collection")
            .WithUserId(userId)
            .WithDescription("Test Description")
            .Build();

        await DbContext.Collections.AddAsync(collection);
        await DbContext.SaveChangesAsync();

        _mockPermissions.Setup(p => p.HasCollectionAccessAsync(1, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var service = CreateCollectionService();

        // Act
        var result = await service.GetByIdAsync(1, userId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(1);
        result.Name.Should().Be("Test Collection");
        result.Description.Should().Be("Test Description");
        result.UserId.Should().Be(userId);
        
        _mockPermissions.Verify(p => p.HasCollectionAccessAsync(1, userId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_WithInvalidId_ReturnsNull()
    {
        // Arrange
        var userId = "test-user";
        var service = CreateCollectionService();

        _mockPermissions.Setup(p => p.HasCollectionAccessAsync(999, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await service.GetByIdAsync(999, userId);

        // Assert
        result.Should().BeNull();
        _mockPermissions.Verify(p => p.HasCollectionAccessAsync(999, userId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithValidData_CreatesCollection()
    {
        // Arrange
        var userId = "test-user";
        var createDto = new CreateCollectionDto
        {
            Name = "New Collection",
            Description = "New Description",
            ParentId = null
        };

        var service = CreateCollectionService();

        // Act
        var result = await service.CreateAsync(createDto, userId);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("New Collection");
        result.Description.Should().Be("New Description");
        result.UserId.Should().Be(userId);
        result.ParentId.Should().BeNull();
        result.Id.Should().BeGreaterThan(0);

        // Verify in database
        var dbCollection = await DbContext.Collections.FindAsync(result.Id);
        dbCollection.Should().NotBeNull();
        dbCollection!.Name.Should().Be("New Collection");
        
        _mockNotifier.Verify(n => n.NotifyCollectionCreatedAsync(It.IsAny<Collection>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithParentId_CreatesChildCollection()
    {
        // Arrange
        var userId = "test-user";
        var parentCollection = new CollectionBuilder()
            .WithId(1)
            .WithName("Parent Collection")
            .WithUserId(userId)
            .Build();

        await DbContext.Collections.AddAsync(parentCollection);
        await DbContext.SaveChangesAsync();

        var createDto = new CreateCollectionDto
        {
            Name = "Child Collection",
            Description = "Child Description",
            ParentId = 1
        };

        _mockPermissions.Setup(p => p.HasCollectionEditAccessAsync(1, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var service = CreateCollectionService();

        // Act
        var result = await service.CreateAsync(createDto, userId);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Child Collection");
        result.ParentId.Should().Be(1);
        result.UserId.Should().Be(userId);
        
        _mockPermissions.Verify(p => p.HasCollectionEditAccessAsync(1, userId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WithValidData_UpdatesCollection()
    {
        // Arrange
        var userId = "test-user";
        var collection = new CollectionBuilder()
            .WithId(1)
            .WithName("Original Name")
            .WithDescription("Original Description")
            .WithUserId(userId)
            .Build();

        await DbContext.Collections.AddAsync(collection);
        await DbContext.SaveChangesAsync();

        var updateDto = new UpdateCollectionDto
        {
            Name = "Updated Name",
            Description = "Updated Description"
        };

        _mockPermissions.Setup(p => p.HasCollectionEditAccessAsync(1, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var service = CreateCollectionService();

        // Act
        var result = await service.UpdateAsync(1, updateDto, userId);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(1);
        result.Name.Should().Be("Updated Name");
        result.Description.Should().Be("Updated Description");

        // Verify in database
        var dbCollection = await DbContext.Collections.FindAsync(1);
        dbCollection.Should().NotBeNull();
        dbCollection!.Name.Should().Be("Updated Name");
        dbCollection.Description.Should().Be("Updated Description");
        
        _mockPermissions.Verify(p => p.HasCollectionEditAccessAsync(1, userId, It.IsAny<CancellationToken>()), Times.Once);
        _mockNotifier.Verify(n => n.NotifyCollectionUpdatedAsync(It.IsAny<Collection>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WithValidId_DeletesCollection()
    {
        // Arrange
        var userId = "test-user";
        var collection = new CollectionBuilder()
            .WithId(1)
            .WithName("Test Collection")
            .WithUserId(userId)
            .Build();

        await DbContext.Collections.AddAsync(collection);
        await DbContext.SaveChangesAsync();

        _mockPermissions.Setup(p => p.HasCollectionDeleteAccessAsync(1, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var service = CreateCollectionService();

        // Act
        var result = await service.DeleteAsync(1, userId);

        // Assert
        result.Should().BeTrue();

        // Verify deleted from database
        var dbCollection = await DbContext.Collections.FindAsync(1);
        dbCollection.Should().BeNull();
        
        _mockPermissions.Verify(p => p.HasCollectionDeleteAccessAsync(1, userId, It.IsAny<CancellationToken>()), Times.Once);
        _mockNotifier.Verify(n => n.NotifyCollectionDeletedAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WithInvalidId_ReturnsFalse()
    {
        // Arrange
        var userId = "test-user";
        var service = CreateCollectionService();

        _mockPermissions.Setup(p => p.HasCollectionDeleteAccessAsync(999, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await service.DeleteAsync(999, userId);

        // Assert
        result.Should().BeFalse();
        _mockPermissions.Verify(p => p.HasCollectionDeleteAccessAsync(999, userId, It.IsAny<CancellationToken>()), Times.Once);
    }
}