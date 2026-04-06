# 🎯 VAH.Backend.Tests - Quick Start Guide

## Current Status: ✅ ALL FILES CREATED

All test project files have been successfully generated in:
```
b:\1A.worktrees\copilot-worktree-2026-04-05T11-26-11\
```

---

## 📦 Files Ready to Deploy

### Core Test Infrastructure Files
✅ **VAH.Backend.Tests.csproj** - Complete project configuration with all NuGet packages
✅ **GlobalUsings.cs** - Global using statements for all test files
✅ **TestFixture.cs** - Base test class with in-memory database setup
✅ **ServiceBuilder.cs** - Fluent DI builder for tests
✅ **AssetBuilder.cs** - Test data builder for Asset entities
✅ **CollectionBuilder.cs** - Test data builder for Collection entities
✅ **UserBuilder.cs** - Test data builder for User entities
✅ **launchSettings.json** - Launch settings

### Setup Automation Scripts
✅ **setup-test-project.bat** - Batch script to organize all files
✅ **setup-test-project.ps1** - PowerShell script to organize all files

### Documentation
✅ **SETUP_TEST_PROJECT.md** - Complete setup and usage guide
✅ **PROJECT_CREATION_SUMMARY.md** - Detailed creation report

---

## 🚀 Three-Step Setup Process

### Step 1: Organize Files (1 minute)

Run the setup script to move all files into the proper directory structure:

**Windows Command Prompt:**
```cmd
cd b:\1A.worktrees\copilot-worktree-2026-04-05T11-26-11
setup-test-project.bat
```

**Windows PowerShell:**
```powershell
cd "b:\1A.worktrees\copilot-worktree-2026-04-05T11-26-11"
.\setup-test-project.ps1
```

**Git Bash:**
```bash
cd "b:\1A.worktrees\copilot-worktree-2026-04-05T11-26-11"
bash setup-test-project.bat
```

After running the script, you should see:
```
VAH.Backend.Tests project structure created successfully!

Directory Structure:
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

### Step 2: Restore Dependencies (2-3 minutes)

```bash
cd VAH.Backend.Tests
dotnet restore
```

This will download all NuGet packages including:
- xUnit 2.7.2
- Moq 4.20.71
- FluentAssertions 6.12.2
- Entity Framework Core 9.*
- ASP.NET Core testing utilities

### Step 3: Add to Solution (30 seconds)

```bash
cd ..
dotnet sln add VAH.Backend.Tests/VAH.Backend.Tests.csproj
```

Or manually edit `VAH.sln` and add:
```xml
Project("{9A19103F-16F7-4668-BE54-9A1E7A4F7556}") = "VAH.Backend.Tests", "VAH.Backend.Tests\VAH.Backend.Tests.csproj", "{YOUR-GUID}"
EndProject
```

---

## ✅ Verify Installation

After completing the 3 steps above, verify everything works:

```bash
# Navigate to test project
cd VAH.Backend.Tests

# Build the project
dotnet build

# Expected output:
# Build succeeded. 0 Warning(s)
```

---

## 🎓 Write Your First Test

Create a file: `VAH.Backend.Tests\Unit\ExampleTests.cs`

```csharp
namespace VAH.Backend.Tests.Unit;

public class ExampleTests : TestFixture
{
    [Fact]
    public async Task Example_WithTestData_ShouldWork()
    {
        // Arrange
        var user = new UserBuilder()
            .WithUserName("testuser")
            .WithEmail("test@example.com")
            .Build();

        // Act
        await DbContext.Users.AddAsync(user);
        await SaveChangesAsync();

        // Assert
        var savedUser = await DbContext.Users
            .FirstOrDefaultAsync(u => u.UserName == "testuser");
        
        savedUser.Should().NotBeNull();
        savedUser!.Email.Should().Be("test@example.com");
    }
}
```

Run the test:
```bash
dotnet test
```

---

## 📋 Project Structure Overview

```
VAH.Backend.Tests/
│
├── 📄 VAH.Backend.Tests.csproj
│   └── Contains all NuGet package references and project settings
│
├── 📄 GlobalUsings.cs
│   └── Global imports: xUnit, Moq, FluentAssertions, EF Core, DI
│
├── 📁 Fixtures/
│   └── TestFixture.cs - Base class for all tests
│       ├── Automatic database setup/teardown
│       ├── Service provider initialization
│       ├── Identity configuration
│       └── Helper methods
│
├── 📁 Builders/
│   ├── ServiceBuilder.cs - DI configuration builder
│   ├── AssetBuilder.cs - Asset test data builder
│   ├── CollectionBuilder.cs - Collection test data builder
│   └── UserBuilder.cs - User test data builder
│
├── 📁 Unit/
│   ├── Services/
│   │   └── YourServiceTests.cs (to be created)
│   ├── Controllers/
│   │   └── YourControllerTests.cs (to be created)
│   └── ...
│
├── 📁 Integration/
│   ├── YourApiTests.cs (to be created)
│   └── ...
│
└── 📁 Properties/
    └── launchSettings.json
```

---

## 🔧 Available Test Utilities

### TestFixture Base Class
```csharp
public abstract class TestFixture : IAsyncLifetime
{
    // Properties
    protected VahDbContext DbContext;              // In-memory database
    protected IServiceScope ServiceScope;          // Service scope
    protected ServiceProvider ServiceProvider;     // DI container
    
    // Lifecycle
    public virtual Task InitializeAsync();         // Before each test
    public virtual Task DisposeAsync();            // After each test
    
    // Helpers
    protected async Task SaveChangesAsync();       // Save changes to DB
    protected async Task ClearDatabaseAsync();     // Clear all data
}
```

### ServiceBuilder Class
```csharp
var provider = new ServiceBuilder()
    .AddService<IAssetService, AssetService>()
    .AddMockService<IStorageService>()
    .AddSingleton<ICacheService, CacheService>()
    .Build();
```

### Test Data Builders
```csharp
// Asset
var asset = new AssetBuilder()
    .WithName("My Asset")
    .WithFileType("image/jpeg")
    .AsPublic()
    .Build();

// Collection
var collection = new CollectionBuilder()
    .WithName("My Collection")
    .WithOwnerId(userId)
    .AsPublic()
    .Build();

// User
var user = new UserBuilder()
    .WithUserName("testuser")
    .WithEmail("test@example.com")
    .WithEmailConfirmed(true)
    .Build();
```

---

## 📚 Common Test Patterns

### Pattern 1: Service Unit Test
```csharp
[Fact]
public async Task GetAsset_WithValidId_ReturnsAsset()
{
    // Arrange
    var assetId = Guid.NewGuid();
    var asset = new AssetBuilder().WithId(assetId).Build();
    
    // Act
    var result = await _service.GetAsync(assetId);
    
    // Assert
    result.Should().NotBeNull();
}
```

### Pattern 2: Database Test
```csharp
[Fact]
public async Task CreateAsset_SavesSuccessfully()
{
    // Arrange
    var asset = new AssetBuilder().WithName("Test").Build();
    
    // Act
    await DbContext.Assets.AddAsync(asset);
    await SaveChangesAsync();
    
    // Assert
    var saved = await DbContext.Assets.FirstOrDefaultAsync();
    saved.Should().NotBeNull();
}
```

### Pattern 3: Mock Service Test
```csharp
[Fact]
public void CallService_WithMockedDependency()
{
    // Arrange
    var mockStorage = new Mock<IStorageService>();
    mockStorage.Setup(x => x.Upload(It.IsAny<Stream>()))
        .ReturnsAsync("file-url");
    
    var service = new AssetService(mockStorage.Object);
    
    // Act
    var result = service.Upload(stream);
    
    // Assert
    result.Should().Be("file-url");
    mockStorage.Verify(x => x.Upload(It.IsAny<Stream>()), Times.Once);
}
```

---

## 🎯 Next Steps

1. ✅ **You are here** - All files created and ready
2. 📁 Run setup script to organize files into `VAH.Backend.Tests/` directory
3. 📦 Run `dotnet restore` in the test project directory
4. 🔗 Run `dotnet sln add VAH.Backend.Tests/VAH.Backend.Tests.csproj`
5. ✔️ Run `dotnet build` to verify everything compiles
6. 🧪 Create your first test file in `Unit/` folder
7. ▶️ Run `dotnet test` to execute your tests

---

## 🚨 Troubleshooting

### "Project file not found"
**Issue:** Setup script didn't run
**Solution:** Run `setup-test-project.bat` from the working directory

### "Restore failed"
**Issue:** NuGet packages couldn't be downloaded
**Solution:** Check internet connection, try `dotnet restore --force`

### "Build failed"
**Issue:** Compilation errors
**Solution:** Verify VAH.Backend.csproj exists and builds successfully first

### "DbContext not found"
**Issue:** TestFixture.cs can't find VahDbContext
**Solution:** Update namespace in TestFixture.cs to match your project

---

## 📞 Key Files Explained

| File | Purpose | Key Features |
|------|---------|--------------|
| VAH.Backend.Tests.csproj | Project configuration | xUnit, Moq, FluentAssertions, EF InMemory |
| GlobalUsings.cs | Common imports | Reduces boilerplate in test files |
| TestFixture.cs | Base test class | Automatic DB setup, service provider, lifecycle |
| ServiceBuilder.cs | DI configuration | Fluent builder for test service setup |
| AssetBuilder.cs | Test data | Fluent builder for Asset entities |
| CollectionBuilder.cs | Test data | Fluent builder for Collection entities |
| UserBuilder.cs | Test data | Fluent builder for User entities |

---

## 📖 Documentation

For detailed information, see:
- **SETUP_TEST_PROJECT.md** - Complete setup guide with examples
- **PROJECT_CREATION_SUMMARY.md** - Detailed creation report

---

**Ready to go!** 🚀 Run the setup script and start testing!
