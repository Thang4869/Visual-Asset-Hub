# VAH.Backend.Tests - File Contents Reference

This document contains the complete content of all created test project files for reference and validation.

---

## 1. VAH.Backend.Tests.csproj

**Location:** `VAH.Backend.Tests/VAH.Backend.Tests.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>

  <!-- Test Frameworks and Utilities -->
  <ItemGroup>
    <PackageReference Include="xunit" Version="2.7.2" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.5.6">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
    <PackageReference Include="Microsoft.NET.Test.SDK" Version="17.13.1" />
  </ItemGroup>

  <!-- Mocking and Assertions -->
  <ItemGroup>
    <PackageReference Include="Moq" Version="4.20.71" />
    <PackageReference Include="FluentAssertions" Version="6.12.2" />
  </ItemGroup>

  <!-- Entity Framework Testing -->
  <ItemGroup>
    <PackageReference Include="Microsoft.EntityFrameworkCore.InMemory" Version="9.*" />
    <PackageReference Include="Microsoft.EntityFrameworkCore" Version="9.*" />
  </ItemGroup>

  <!-- ASP.NET Core Testing -->
  <ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="9.*" />
    <PackageReference Include="Microsoft.AspNetCore.Identity.EntityFrameworkCore" Version="9.*" />
  </ItemGroup>

  <!-- Dependency Injection -->
  <ItemGroup>
    <PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="9.*" />
  </ItemGroup>

  <!-- Project References -->
  <ItemGroup>
    <ProjectReference Include="..\VAH.Backend\VAH.Backend.csproj" />
  </ItemGroup>

</Project>
```

**Key Points:**
- .NET 9 target framework
- All test dependencies included
- Project reference to VAH.Backend
- Marked as test project

---

## 2. GlobalUsings.cs

**Location:** `VAH.Backend.Tests/GlobalUsings.cs`

```csharp
global using System;
global using System.Collections.Generic;
global using System.Linq;
global using System.Threading.Tasks;
global using FluentAssertions;
global using Moq;
global using Xunit;
global using Microsoft.EntityFrameworkCore;
global using Microsoft.Extensions.DependencyInjection;
```

**Provides Global Access To:**
- System namespaces (no need to import)
- FluentAssertions for readable assertions
- Moq for mocking
- xUnit for test framework
- Entity Framework Core
- Dependency injection services

---

## 3. Fixtures/TestFixture.cs

**Location:** `VAH.Backend.Tests/Fixtures/TestFixture.cs`

```csharp
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using VAH.Backend.Data;

namespace VAH.Backend.Tests.Fixtures;

/// <summary>
/// Base test fixture providing in-memory database setup and common test utilities.
/// </summary>
public abstract class TestFixture : IAsyncLifetime
{
    protected readonly ServiceProvider ServiceProvider;
    protected readonly IServiceScope ServiceScope;
    protected readonly VahDbContext DbContext;

    protected TestFixture()
    {
        var services = new ServiceCollection();
        ConfigureServices(services);
        
        ServiceProvider = services.BuildServiceProvider();
        ServiceScope = ServiceProvider.CreateScope();
        DbContext = ServiceScope.ServiceProvider.GetRequiredService<VahDbContext>();
    }

    /// <summary>
    /// Override this method to configure additional services for the test.
    /// </summary>
    protected virtual void ConfigureServices(IServiceCollection services)
    {
        services.AddDbContext<VahDbContext>(options =>
            options.UseInMemoryDatabase(Guid.NewGuid().ToString())
        );

        services.AddIdentityCore<IdentityUser>(options =>
        {
            options.Password.RequireDigit = false;
            options.Password.RequireLowercase = false;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequireUppercase = false;
            options.Password.RequiredLength = 1;
        })
        .AddEntityFrameworkStores<VahDbContext>();
    }

    /// <summary>
    /// Called when the fixture is initialized (before test runs).
    /// </summary>
    public virtual Task InitializeAsync()
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Called when the fixture is disposed (after test runs).
    /// </summary>
    public virtual async Task DisposeAsync()
    {
        await DbContext.Database.EnsureDeletedAsync();
        await ServiceScope.DisposeAsync();
        ServiceProvider.Dispose();
    }

    /// <summary>
    /// Saves all pending changes to the database.
    /// </summary>
    protected async Task SaveChangesAsync()
    {
        await DbContext.SaveChangesAsync();
    }

    /// <summary>
    /// Clears all data from the database.
    /// </summary>
    protected async Task ClearDatabaseAsync()
    {
        await DbContext.Database.EnsureDeletedAsync();
        await DbContext.Database.EnsureCreatedAsync();
    }
}
```

**Key Features:**
- Implements IAsyncLifetime for proper lifecycle management
- Creates fresh in-memory database for each test (unique GUID)
- Configures Identity with relaxed password requirements
- Protected ServiceProvider, ServiceScope, and DbContext
- Helper methods: SaveChangesAsync(), ClearDatabaseAsync()

**Usage:**
```csharp
public class MyTests : TestFixture
{
    [Fact]
    public async Task MyTest()
    {
        // DbContext is available automatically
        // ServiceProvider is available for DI
    }
}
```

---

## 4. Builders/ServiceBuilder.cs

**Location:** `VAH.Backend.Tests/Builders/ServiceBuilder.cs`

```csharp
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using VAH.Backend.Data;

namespace VAH.Backend.Tests.Builders;

/// <summary>
/// Builder for configuring and creating test service providers with dependency injection.
/// Uses the builder pattern for flexible test setup.
/// </summary>
public class ServiceBuilder
{
    private readonly IServiceCollection _services;

    public ServiceBuilder()
    {
        _services = new ServiceCollection();
        ConfigureDefaults();
    }

    /// <summary>
    /// Configures default services required for testing.
    /// </summary>
    private void ConfigureDefaults()
    {
        // Add in-memory database
        _services.AddDbContext<VahDbContext>(options =>
            options.UseInMemoryDatabase(Guid.NewGuid().ToString())
        );

        // Add Identity
        _services.AddIdentityCore<IdentityUser>(options =>
        {
            options.Password.RequireDigit = false;
            options.Password.RequireLowercase = false;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequireUppercase = false;
            options.Password.RequiredLength = 1;
        })
        .AddEntityFrameworkStores<VahDbContext>();
    }

    /// <summary>
    /// Adds a service to the dependency injection container.
    /// </summary>
    public ServiceBuilder AddService<TInterface, TImplementation>() 
        where TInterface : class 
        where TImplementation : class, TInterface
    {
        _services.AddScoped<TInterface, TImplementation>();
        return this;
    }

    /// <summary>
    /// Adds a singleton service to the dependency injection container.
    /// </summary>
    public ServiceBuilder AddSingleton<TInterface, TImplementation>()
        where TInterface : class
        where TImplementation : class, TInterface
    {
        _services.AddSingleton<TInterface, TImplementation>();
        return this;
    }

    /// <summary>
    /// Adds a mocked service to the dependency injection container.
    /// </summary>
    public ServiceBuilder AddMockService<TInterface>() where TInterface : class
    {
        var mock = new Mock<TInterface>();
        _services.AddScoped(_ => mock.Object);
        return this;
    }

    /// <summary>
    /// Adds a mocked service and returns the mock for verification.
    /// </summary>
    public ServiceBuilder AddMockService<TInterface>(out Mock<TInterface> mock) 
        where TInterface : class
    {
        mock = new Mock<TInterface>();
        _services.AddScoped(_ => mock.Object);
        return this;
    }

    /// <summary>
    /// Builds the service provider with the configured services.
    /// </summary>
    public IServiceProvider Build()
    {
        return _services.BuildServiceProvider();
    }

    /// <summary>
    /// Gets the configured service collection for advanced customization.
    /// </summary>
    public IServiceCollection GetServices() => _services;
}
```

**Methods:**
- `AddService<I, T>()` - Add scoped service
- `AddSingleton<I, T>()` - Add singleton service
- `AddMockService<I>()` - Add mocked service (disposable)
- `AddMockService<I>(out Mock<I>)` - Add mocked service with mock reference
- `Build()` - Build IServiceProvider
- `GetServices()` - Access IServiceCollection for advanced config

---

## 5. Builders/AssetBuilder.cs

**Location:** `VAH.Backend.Tests/Builders/AssetBuilder.cs`

```csharp
namespace VAH.Backend.Tests.Builders;

/// <summary>
/// Builder for creating test Asset entities using the builder pattern.
/// </summary>
public class AssetBuilder
{
    private Guid _id = Guid.NewGuid();
    private string _name = "Test Asset";
    private string _description = "Test Asset Description";
    private string _filePath = "/test/path/asset.jpg";
    private string _fileType = "image/jpeg";
    private long _fileSize = 1024;
    private Guid _collectionId = Guid.NewGuid();
    private Guid _uploadedBy = Guid.NewGuid();
    private DateTime _uploadedAt = DateTime.UtcNow;
    private bool _isPublic = false;

    public AssetBuilder WithId(Guid id)
    {
        _id = id;
        return this;
    }

    public AssetBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public AssetBuilder WithDescription(string description)
    {
        _description = description;
        return this;
    }

    public AssetBuilder WithFilePath(string filePath)
    {
        _filePath = filePath;
        return this;
    }

    public AssetBuilder WithFileType(string fileType)
    {
        _fileType = fileType;
        return this;
    }

    public AssetBuilder WithFileSize(long fileSize)
    {
        _fileSize = fileSize;
        return this;
    }

    public AssetBuilder WithCollectionId(Guid collectionId)
    {
        _collectionId = collectionId;
        return this;
    }

    public AssetBuilder WithUploadedBy(Guid userId)
    {
        _uploadedBy = userId;
        return this;
    }

    public AssetBuilder WithUploadedAt(DateTime uploadedAt)
    {
        _uploadedAt = uploadedAt;
        return this;
    }

    public AssetBuilder AsPublic()
    {
        _isPublic = true;
        return this;
    }

    public AssetBuilder AsPrivate()
    {
        _isPublic = false;
        return this;
    }

    /// <summary>
    /// Builds and returns the Asset entity.
    /// Note: Update this to match your actual Asset model.
    /// </summary>
    public dynamic Build()
    {
        return new
        {
            Id = _id,
            Name = _name,
            Description = _description,
            FilePath = _filePath,
            FileType = _fileType,
            FileSize = _fileSize,
            CollectionId = _collectionId,
            UploadedBy = _uploadedBy,
            UploadedAt = _uploadedAt,
            IsPublic = _isPublic
        };
    }
}
```

**Default Values:**
- Id: Guid.NewGuid()
- Name: "Test Asset"
- Description: "Test Asset Description"
- FilePath: "/test/path/asset.jpg"
- FileType: "image/jpeg"
- FileSize: 1024
- IsPublic: false

**Methods:** All properties have WithX() methods and AsPublic()/AsPrivate() shortcuts

---

## 6. Builders/CollectionBuilder.cs

**Location:** `VAH.Backend.Tests/Builders/CollectionBuilder.cs`

```csharp
namespace VAH.Backend.Tests.Builders;

/// <summary>
/// Builder for creating test Collection entities using the builder pattern.
/// </summary>
public class CollectionBuilder
{
    private Guid _id = Guid.NewGuid();
    private string _name = "Test Collection";
    private string _description = "Test Collection Description";
    private Guid _ownerId = Guid.NewGuid();
    private DateTime _createdAt = DateTime.UtcNow;
    private DateTime? _updatedAt = null;
    private bool _isPublic = false;
    private int _assetCount = 0;

    public CollectionBuilder WithId(Guid id)
    {
        _id = id;
        return this;
    }

    public CollectionBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public CollectionBuilder WithDescription(string description)
    {
        _description = description;
        return this;
    }

    public CollectionBuilder WithOwnerId(Guid ownerId)
    {
        _ownerId = ownerId;
        return this;
    }

    public CollectionBuilder WithCreatedAt(DateTime createdAt)
    {
        _createdAt = createdAt;
        return this;
    }

    public CollectionBuilder WithUpdatedAt(DateTime? updatedAt)
    {
        _updatedAt = updatedAt;
        return this;
    }

    public CollectionBuilder AsPublic()
    {
        _isPublic = true;
        return this;
    }

    public CollectionBuilder AsPrivate()
    {
        _isPublic = false;
        return this;
    }

    public CollectionBuilder WithAssetCount(int count)
    {
        _assetCount = count;
        return this;
    }

    /// <summary>
    /// Builds and returns the Collection entity.
    /// Note: Update this to match your actual Collection model.
    /// </summary>
    public dynamic Build()
    {
        return new
        {
            Id = _id,
            Name = _name,
            Description = _description,
            OwnerId = _ownerId,
            CreatedAt = _createdAt,
            UpdatedAt = _updatedAt,
            IsPublic = _isPublic,
            AssetCount = _assetCount
        };
    }
}
```

**Default Values:**
- Id: Guid.NewGuid()
- Name: "Test Collection"
- Description: "Test Collection Description"
- OwnerId: Guid.NewGuid()
- CreatedAt: DateTime.UtcNow
- UpdatedAt: null
- IsPublic: false
- AssetCount: 0

---

## 7. Builders/UserBuilder.cs

**Location:** `VAH.Backend.Tests/Builders/UserBuilder.cs`

```csharp
using Microsoft.AspNetCore.Identity;

namespace VAH.Backend.Tests.Builders;

/// <summary>
/// Builder for creating test IdentityUser entities using the builder pattern.
/// </summary>
public class UserBuilder
{
    private readonly IdentityUser _user;

    public UserBuilder()
    {
        _user = new IdentityUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = "testuser",
            Email = "testuser@example.com",
            EmailConfirmed = true,
            NormalizedEmail = "TESTUSER@EXAMPLE.COM",
            NormalizedUserName = "TESTUSER",
            SecurityStamp = Guid.NewGuid().ToString()
        };
    }

    public UserBuilder WithId(string id)
    {
        _user.Id = id;
        return this;
    }

    public UserBuilder WithUserName(string userName)
    {
        _user.UserName = userName;
        _user.NormalizedUserName = userName.ToUpper();
        return this;
    }

    public UserBuilder WithEmail(string email)
    {
        _user.Email = email;
        _user.NormalizedEmail = email.ToUpper();
        return this;
    }

    public UserBuilder WithEmailConfirmed(bool confirmed)
    {
        _user.EmailConfirmed = confirmed;
        return this;
    }

    public UserBuilder WithPhoneNumber(string phoneNumber)
    {
        _user.PhoneNumber = phoneNumber;
        return this;
    }

    public UserBuilder WithPhoneNumberConfirmed(bool confirmed)
    {
        _user.PhoneNumberConfirmed = confirmed;
        return this;
    }

    public UserBuilder WithTwoFactorEnabled(bool enabled)
    {
        _user.TwoFactorEnabled = enabled;
        return this;
    }

    public UserBuilder WithLockoutEnd(DateTimeOffset? lockoutEnd)
    {
        _user.LockoutEnd = lockoutEnd;
        return this;
    }

    public UserBuilder WithLockoutEnabled(bool enabled)
    {
        _user.LockoutEnabled = enabled;
        return this;
    }

    /// <summary>
    /// Builds and returns the IdentityUser entity.
    /// </summary>
    public IdentityUser Build()
    {
        return _user;
    }
}
```

**Default Values:**
- Id: Guid.NewGuid().ToString()
- UserName: "testuser"
- Email: "testuser@example.com"
- EmailConfirmed: true
- NormalizedEmail: "TESTUSER@EXAMPLE.COM"
- NormalizedUserName: "TESTUSER"
- SecurityStamp: Guid.NewGuid().ToString()

---

## 8. Properties/launchSettings.json

**Location:** `VAH.Backend.Tests/Properties/launchSettings.json`

```json
{
  "profiles": {
    "VAH.Backend.Tests": {
      "commandName": "Project"
    }
  }
}
```

---

## Summary

All files follow best practices:
- ✅ Comprehensive XML documentation
- ✅ Fluent builder patterns
- ✅ Sensible default values
- ✅ Proper namespaces
- ✅ Global using statements
- ✅ Clean separation of concerns
- ✅ Full NuGet dependency coverage
- ✅ .NET 9 compatibility

The test infrastructure is ready to support comprehensive unit and integration testing of the VAH.Backend project.
