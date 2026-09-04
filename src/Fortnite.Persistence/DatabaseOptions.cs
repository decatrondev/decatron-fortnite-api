namespace Fortnite.Persistence;

/// <summary>Config de la base. Se liga de la sección "Database".</summary>
public sealed record DatabaseOptions
{
    /// <summary>Cadena de conexión Npgsql. Vacío = el ingest no toca la base.</summary>
    public string ConnectionString { get; init; } = "";

    public bool Enabled => !string.IsNullOrWhiteSpace(ConnectionString);
}
