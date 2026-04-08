namespace VAH.Backend.Extensions;

/// <summary>
/// Extension methods for running <see cref="IStartupInitializer"/> instances at app startup.
/// </summary>
public static class StartupInitializerExtensions
{
    /// <summary>
    /// Register the database migration initializer.
    /// Add more <see cref="IStartupInitializer"/> registrations here as needed
    /// (e.g. default admin seeding, cache warm-up).
    /// </summary>
    public static IServiceCollection AddStartupInitializers(this IServiceCollection services, IHostEnvironment env)
    {
        // Avoid fully automated background migrations in production by default.
        // Set MIGRATION_ON_STARTUP=true in Env to force it.
        var runMigration = env.IsDevelopment() || Environment.GetEnvironmentVariable("MIGRATION_ON_STARTUP") == "true";
        if (runMigration)
        {
            services.AddScoped<IStartupInitializer, DatabaseMigrationInitializer>();
        }

        return services;
    }

    /// <summary>
    /// Resolves and runs all registered <see cref="IStartupInitializer"/> implementations
    /// in <see cref="IStartupInitializer.Order"/> sequence.
    /// </summary>
    public static async Task RunStartupInitializersAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var initializers = scope.ServiceProvider
            .GetServices<IStartupInitializer>()
            .OrderBy(i => i.Order);

        foreach (var initializer in initializers)
        {
            await initializer.InitializeAsync();
        }
    }
}
