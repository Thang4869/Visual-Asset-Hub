# VAH.Backend.Tests - Manual Setup Instructions

## Quick Setup (5 minutes)

Vì PowerShell tools không available, bạn cần run manual setup:

### Step 1: Organize Files (2 minutes)
```batch
cd b:\1A.worktrees\copilot-worktree-2026-04-05T11-26-11
setup-test-project.bat
```

**Hoặc manual:**
```batch
mkdir VAH.Backend.Tests
mkdir VAH.Backend.Tests\Fixtures
mkdir VAH.Backend.Tests\Builders
mkdir VAH.Backend.Tests\Unit
mkdir VAH.Backend.Tests\Integration
mkdir VAH.Backend.Tests\Properties

move VAH.Backend.Tests.csproj VAH.Backend.Tests\
move GlobalUsings.cs VAH.Backend.Tests\
move TestFixture.cs VAH.Backend.Tests\Fixtures\
move ServiceBuilder.cs VAH.Backend.Tests\Builders\
move AssetBuilder.cs VAH.Backend.Tests\Builders\
move CollectionBuilder.cs VAH.Backend.Tests\Builders\
move UserBuilder.cs VAH.Backend.Tests\Builders\
move launchSettings.json VAH.Backend.Tests\Properties\
```

### Step 2: Add to Solution (1 minute)
```bash
dotnet sln add VAH.Backend.Tests/VAH.Backend.Tests.csproj
```

### Step 3: Restore & Build (2 minutes)
```bash
cd VAH.Backend.Tests
dotnet restore
dotnet build
cd ..
```

### Step 4: Verify Tests Work
```bash
cd VAH.Backend.Tests
dotnet test --dry-run
```

## Expected Directory Structure After Setup:

```
VAH.Backend.Tests/
├── VAH.Backend.Tests.csproj     ← Project file with xUnit, Moq, FluentAssertions
├── GlobalUsings.cs              ← Global imports (no need for using statements)
├── Fixtures/
│   └── TestFixture.cs          ← Base test class with in-memory database
├── Builders/
│   ├── ServiceBuilder.cs       ← DI container builder for tests
│   ├── AssetBuilder.cs         ← Test data builder for Assets
│   ├── CollectionBuilder.cs    ← Test data builder for Collections
│   └── UserBuilder.cs          ← Test data builder for Users
├── Unit/                       ← Put your unit tests here
├── Integration/                ← Put your integration tests here
└── Properties/
    └── launchSettings.json     ← Launch configuration
```

## After Setup, You Can Write Tests Like This:

```csharp
public class AssetServiceTests : TestFixture
{
    [Fact]
    public async Task CreateAsset_WithValidData_ShouldReturnSuccess()
    {
        // Arrange
        var service = ServiceBuilder.New()
            .WithInMemoryDatabase()
            .WithAssetService()
            .Build<AssetService>();

        var asset = new AssetBuilder()
            .WithName("Test Image")
            .WithFileType("image/jpeg")
            .WithFileSize(1024 * 1024) // 1MB
            .AsPublic()
            .Build();

        // Act
        var result = await service.CreateAsync(asset);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Test Image");
    }
}
```

## 🚀 Next Priority After Setup:

1. **Write AssetService tests** (5 critical methods)
2. **Add CollectionService tests** (hierarchy, permissions)  
3. **Add TagService tests** (M:N relationships)
4. **Add Validator tests** (boundary conditions)

## 📊 Target Coverage:

- **Goal**: 60% service layer coverage before any major refactoring
- **Critical**: AssetService, CollectionService, TagService, AuthService
- **Medium**: Validators, SmartCollectionFilters
- **Nice-to-have**: Controllers (integration tests)