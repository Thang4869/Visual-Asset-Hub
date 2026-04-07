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
        _services.AddDbContext<AppDbContext>(options =>
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
        .AddEntityFrameworkStores<AppDbContext>();
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
        var localMock = mock; _services.AddScoped(_ => localMock.Object);
        return this;
    }

    /// <summary>
    /// Adds a mocked service and returns the mock for verification.
    /// </summary>
    public ServiceBuilder AddMockService<TInterface>(out Mock<TInterface> mock) 
        where TInterface : class
    {
        mock = new Mock<TInterface>();
        var localMock = mock; _services.AddScoped(_ => localMock.Object);
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



