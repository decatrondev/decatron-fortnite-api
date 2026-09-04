using System.Text.Json;

namespace Fortnite.Api.SpriteSource;

/// <summary>
/// Lee data/catalog.json y data/images.json. Cachea en memoria y recarga si cambia el mtime.
/// </summary>
public sealed class FileSpriteSource(string dataRoot, ILogger<FileSpriteSource> logger) : ISpriteSource
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly string _catalogPath = Path.Combine(dataRoot, "catalog.json");
    private readonly string _imagesPath = Path.Combine(dataRoot, "images.json");
    private readonly SemaphoreSlim _lock = new(1, 1);

    private IReadOnlyList<SpriteDto>? _catalog;
    private IReadOnlyDictionary<string, string>? _hashes;
    private DateTime _catalogMtime;
    private DateTime _imagesMtime;

    public async Task<IReadOnlyList<SpriteDto>> GetAllAsync(CancellationToken ct = default)
    {
        await EnsureLoadedAsync(ct);
        return _catalog ?? [];
    }

    public async Task<IReadOnlyDictionary<string, string>> GetImageHashesAsync(CancellationToken ct = default)
    {
        await EnsureLoadedAsync(ct);
        return _hashes ?? new Dictionary<string, string>();
    }

    private async Task EnsureLoadedAsync(CancellationToken ct)
    {
        var catMtime = SafeMtime(_catalogPath);
        var imgMtime = SafeMtime(_imagesPath);
        if (_catalog is not null && catMtime == _catalogMtime && imgMtime == _imagesMtime)
        {
            return;
        }

        await _lock.WaitAsync(ct);
        try
        {
            if (_catalog is not null && catMtime == _catalogMtime && imgMtime == _imagesMtime)
            {
                return;
            }

            if (!File.Exists(_catalogPath))
            {
                logger.LogWarning("No existe {Path}. Catálogo vacío.", _catalogPath);
                _catalog = [];
                _hashes = new Dictionary<string, string>();
                return;
            }

            var catalog = JsonSerializer.Deserialize<List<SpriteDto>>(
                await File.ReadAllTextAsync(_catalogPath, ct), Json) ?? [];

            var hashes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (File.Exists(_imagesPath))
            {
                using var doc = JsonDocument.Parse(await File.ReadAllTextAsync(_imagesPath, ct));
                foreach (var prop in doc.RootElement.EnumerateObject())
                {
                    if (prop.Value.TryGetProperty("hash", out var h) && h.GetString() is { } hv)
                    {
                        hashes[prop.Name] = hv;
                    }
                }
            }

            _catalog = catalog;
            _hashes = hashes;
            _catalogMtime = catMtime;
            _imagesMtime = imgMtime;
            logger.LogInformation("Catálogo cargado: {Count} sprites, {Hashes} hashes.", catalog.Count, hashes.Count);
        }
        finally
        {
            _lock.Release();
        }
    }

    private static DateTime SafeMtime(string path)
    {
        try
        {
            return File.GetLastWriteTimeUtc(path);
        }
        catch
        {
            return DateTime.MinValue;
        }
    }
}
