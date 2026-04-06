# VAH.Backend.Tests Project Setup Guide

## Current Status

✅ **All test project files have been created successfully** in the working directory:
- `b:\1A.worktrees\copilot-worktree-2026-04-05T11-26-11`

However, they need to be organized into the proper directory structure.

## Files Created

The following files are ready in the root directory:

1. **VAH.Backend.Tests.csproj** - Complete project file with all dependencies
2. **GlobalUsings.cs** - Global using statements
3. **TestFixture.cs** - Base test fixture class
4. **ServiceBuilder.cs** - Dependency injection builder
5. **AssetBuilder.cs** - Asset entity builder
6. **CollectionBuilder.cs** - Collection entity builder
7. **UserBuilder.cs** - User entity builder
8. **launchSettings.json** - Launch settings
9. **setup-test-project.bat** - Batch file to organize files
10. **setup-test-project.ps1** - PowerShell script to organize files

## Required Directory Structure

```
VAH.Backend.Tests/
├── VAH.Backend.Tests.csproj
├── GlobalUsings.cs
├── Fixtures/
│   └── TestFixture.cs
├── Builders/
│   ├── ServiceBuilder.cs
│   ├── AssetBuilder.cs
│   ├── CollectionBuilder.cs
│   └── UserBuilder.cs
├── Unit/
│   └── .gitkeep
├── Integration/
│   └── .gitkeep
└── Properties/
    └── launchSettings.json
```

## Setup Instructions

### Option 1: Using Batch Script (Recommended for Windows)

Open Command Prompt and run:

```batch
cd b:\1A.worktrees\copilot-worktree-2026-04-05T11-26-11
setup-test-project.bat
```

### Option 2: Using PowerShell

Open PowerShell and run:

```powershell
cd "b:\1A.worktrees\copilot-worktree-2026-04-05T11-26-11"
.\setup-test-project.ps1
```

### Option 3: Manual Setup

If the scripts don't work, manually create the directories and move the files:

1. **Create Directories:**
   ```
   mkdir VAH.Backend.Tests\Fixtures
   mkdir VAH.Backend.Tests\Builders
   mkdir VAH.Backend.Tests\Unit
   mkdir VAH.Backend.Tests\Integration
   mkdir VAH.Backend.Tests\Properties
   ```

2. **Move Files:**
   ```
   move VAH.Backend.Tests.csproj VAH.Backend.Tests\
   move GlobalUsings.cs VAH.Backend.Tests\
   move TestFixture.cs VAH.Backend.Tests\Fixtures\
   move ServiceBuilder.cs VAH.Backend.Tests\Builders\
   move AssetBuilder.cs VAH.Backend.Tests\Builders\
   move CollectionBuilder.cs VAH.Backend.Tests\Builders\
   move UserBuilder.cs VAH.Backend.Tests\Builders\
   move launchSettings.json VAH.Backend.Tests\Properties\
   ```

3. **Create Empty Files:**
   ```
   echo. > VAH.Backend.Tests\Unit\.gitkeep
   echo. > VAH.Backend.Tests\Integration\.gitkeep
   ```

## Next Steps After Setup

Once the directories are organized:

### 1. Restore NuGet Packages

```bash
cd VAH.Backend.Tests
dotnet restore
```

### 2. Add Project to Solution

```bash
cd ..
dotnet sln add VAH.Backend.Tests/VAH.Backend.Tests.csproj
```

### 3. Verify Setup

```bash
dotnet build VAH.Backend.Tests
```

## Project Features

### ✅ Test Framework
- **xUnit** - Modern test framework for .NET
- **Moq** - Mocking library for unit tests
- **FluentAssertions** - Readable assertion library

### ✅ Database Testing
- **Entity Framework InMemory** - In-memory database provider
- **TestFixture.cs** - Base class with automatic database setup/teardown

### ✅ Dependency Injection
- **ServiceBuilder** - Fluent builder for test service configuration
- Pre-configured Identity Core with relaxed password requirements

### ✅ Test Data Builders
- **AssetBuilder** - Build test Asset entities
- **CollectionBuilder** - Build test Collection entities  
- **UserBuilder** - Build test User entities

### ✅ Global Usings
All test files automatically include:
- System namespaces
- xUnit
- Moq
- FluentAssertions
- EntityFrameworkCore
- Microsoft.Extensions.DependencyInjection

## Test Infrastructure Files

### GlobalUsings.cs
Provides global imports for all test files to reduce boilerplate code.

### Fixtures/TestFixture.cs
**Base class for all test fixtures**

Key features:
- Automatic in-memory database setup
- Automatic service provider initialization
- `InitializeAsync()` - Called before each test
- `DisposeAsync()` - Called after each test
- `SaveChangesAsync()` - Helper to save changes
- `ClearDatabaseAsync()` - Helper to clear test data

Example usage:
```csharp
public class AssetServiceTests : TestFixture
{
    [Fact]
    public async Task CreateAsset_WithValidData_ReturnsSuccess()
    {
        // Arrange
        var asset = new AssetBuilder()
            .WithName("My Asset")
            .Build();

        // Act
        var result = await DbContext.Assets.AddAsync(asset);

        // Assert
        result.Should().NotBeNull();
    }
}
```

### Builders/ServiceBuilder.cs
**Fluent DI configuration builder**

Example usage:
```csharp
var serviceProvider = new ServiceBuilder()
    .AddService<IAssetService, AssetService>()
    .AddSingleton<ICacheService, CacheService>()
    .AddMockService<IEmailService>()
    .Build();
```

### Builders/AssetBuilder.cs
**Fluent test data builder for Assets**

Example usage:
```csharp
var asset = new AssetBuilder()
    .WithName("Test Asset")
    .WithFileType("image/jpeg")
    .WithFileSize(1024)
    .AsPublic()
    .Build();
```

### Builders/CollectionBuilder.cs
**Fluent test data builder for Collections**

Example usage:
```csharp
var collection = new CollectionBuilder()
    .WithName("My Collection")
    .WithOwnerId(userId)
    .AsPublic()
    .WithAssetCount(5)
    .Build();
```

### Builders/UserBuilder.cs
**Fluent test data builder for Identity Users**

Example usage:
```csharp
var user = new UserBuilder()
    .WithUserName("testuser")
    .WithEmail("test@example.com")
    .WithEmailConfirmed(true)
    .Build();
```

## Writing Your First Test

Create a new file: `Unit/AssetServiceTests.cs`

```csharp
namespace VAH.Backend.Tests.Unit;

public class AssetServiceTests : TestFixture
{
    [Fact]
    public async Task CreateAsset_WithValidData_ShouldSucceed()
    {
        // Arrange
        var asset = new AssetBuilder()
            .WithName("Test Image")
            .WithFileType("image/jpeg")
            .Build();

        // Act
        await DbContext.SaveChangesAsync();

        // Assert
        (await DbContext.Assets.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task GetAsset_WithInvalidId_ShouldReturnNull()
    {
        // Act
        var result = await DbContext.Assets.FirstOrDefaultAsync();

        // Assert
        result.Should().BeNull();
    }
}
```

## Package Information

### NuGet Packages Included

| Package | Version | Purpose |
|---------|---------|---------|
| xunit | 2.7.2 | Test framework |
| xunit.runner.visualstudio | 2.5.6 | Visual Studio integration |
| Microsoft.NET.Test.SDK | 17.13.1 | Test SDK |
| Moq | 4.20.71 | Mocking |
| FluentAssertions | 6.12.2 | Assertions |
| Microsoft.EntityFrameworkCore.InMemory | 9.* | In-memory database |
| Microsoft.EntityFrameworkCore | 9.* | EF Core |
| Microsoft.AspNetCore.Mvc.Testing | 9.* | Integration testing |
| Microsoft.AspNetCore.Identity.EntityFrameworkCore | 9.* | Identity support |
| Microsoft.Extensions.DependencyInjection | 9.* | DI container |

## Running Tests

```bash
# Run all tests
dotnet test

# Run tests with verbose output
dotnet test --verbosity normal

# Run specific test class
dotnet test --filter "ClassName=AssetServiceTests"

# Run with code coverage
dotnet test /p:CollectCoverage=true
```

## Project Structure for Development

After setup is complete, you can add test files to the `Unit` and `Integration` folders:

```
VAH.Backend.Tests/
├── Unit/
│   ├── Services/
│   │   ├── AssetServiceTests.cs
│   │   └── CollectionServiceTests.cs
│   ├── Controllers/
│   │   └── AssetControllerTests.cs
│   └── ...
├── Integration/
│   ├── AssetApiTests.cs
│   ├── CollectionApiTests.cs
│   └── ...
├── Fixtures/
│   └── TestFixture.cs
├── Builders/
│   ├── ServiceBuilder.cs
│   ├── AssetBuilder.cs
│   ├── CollectionBuilder.cs
│   └── UserBuilder.cs
└── ...
```

## Troubleshooting

### Issue: "VAH.Backend not found"
**Solution:** Ensure `dotnet restore` has been run to download all dependencies.

### Issue: "VahDbContext not found"
**Solution:** Update the imports in TestFixture.cs to match your actual DbContext namespace and name.

### Issue: "Identity setup fails"
**Solution:** The TestFixture.cs configures relaxed password requirements. Adjust the password policy in `ConfigureServices` if needed.

### Issue: Tests don't run
**Solution:** 
1. Ensure the project has been added to the solution
2. Run `dotnet build` to verify no compilation errors
3. Check that test files are in `Unit` or `Integration` folders
4. Verify test class names end with "Tests" and methods are marked with `[Fact]` or `[Theory]`

## Additional Resources

- [xUnit Documentation](https://xunit.net/)
- [Moq Documentation](https://github.com/moq/moq4)
- [FluentAssertions Documentation](https://fluentassertions.com/)
- [Entity Framework Testing](https://docs.microsoft.com/en-us/ef/core/testing/)
- [ASP.NET Core Testing](https://docs.microsoft.com/en-us/aspnet/core/test/index)

## Support

If you encounter issues:
1. Check that all files are in the correct directories
2. Verify the project has been added to VAH.sln
3. Run `dotnet clean` and `dotnet build` to rebuild
4. Check that the VAH.Backend project builds successfully first
