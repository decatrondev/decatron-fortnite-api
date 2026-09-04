namespace Fortnite.Api.SpriteSource;

/// <summary>Origen de datos de sprites. Dos implementaciones: archivo (data/) y PostgreSQL.</summary>
public interface ISpriteSource
{
    /// <summary>Catálogo completo.</summary>
    Task<IReadOnlyList<SpriteDto>> GetAllAsync(CancellationToken ct = default);

    /// <summary>Hash de imagen por id, para el parámetro ?v= de ruptura de caché. Puede faltar.</summary>
    Task<IReadOnlyDictionary<string, string>> GetImageHashesAsync(CancellationToken ct = default);
}
