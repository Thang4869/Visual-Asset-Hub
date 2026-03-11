namespace VAH.Backend.Controllers;

/// <summary>
/// Centralized route path constants for endpoints registered outside controllers.
/// Eliminates magic strings in <c>Program.cs</c> (MapHub, MapHealthChecks, etc.).
/// </summary>
internal static class RouteConstants
{
    public const string AssetHub = "/hubs/assets";
    public const string HealthLive = "/health/live";
    public const string HealthReady = "/health/ready";
    public const string Version = "/version";
}
