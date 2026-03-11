namespace VAH.Backend.Data;

/// <summary>Exposes the configured DB provider name so AppDbContext can adapt SQL dialect.</summary>
public record DatabaseProviderInfo(string ProviderName)
{
    public bool IsPostgreSql => ProviderName.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase);
    public bool IsSqlite => !IsPostgreSql;
}
