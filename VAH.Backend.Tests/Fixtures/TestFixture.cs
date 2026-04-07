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
    protected readonly AppDbContext DbContext;

    protected TestFixture()
    {
        var services = new ServiceCollection();
        ConfigureServices(services);
        
        ServiceProvider = services.BuildServiceProvider();
        ServiceScope = ServiceProvider.CreateScope();
        DbContext = ServiceScope.ServiceProvider.GetRequiredService<AppDbContext>();
    }

    /// <summary>
    /// Override this method to configure additional services for the test.
    /// </summary>
    protected virtual void ConfigureServices(IServiceCollection services)
    {
        services.AddDbContext<AppDbContext>(options =>
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
        .AddEntityFrameworkStores<AppDbContext>();
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
        if (ServiceScope is IAsyncDisposable ad) await ad.DisposeAsync(); else ServiceScope.Dispose();
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


