namespace VAH.Backend.Extensions;

/// <summary>
/// Contract for components that perform one-time async initialization at app startup
/// (e.g. database migration, cache warm-up, default admin seeding).
/// <para>Registered via DI and executed in order by
/// <see cref="StartupInitializerExtensions.RunStartupInitializersAsync"/>.</para>
/// </summary>
public interface IStartupInitializer
{
    int Order => 0;
    Task InitializeAsync(CancellationToken ct = default);
}
