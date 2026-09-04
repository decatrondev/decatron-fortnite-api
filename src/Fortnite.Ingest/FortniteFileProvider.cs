using CUE4Parse.Encryption.Aes;
using CUE4Parse.FileProvider;
using CUE4Parse.MappingsProvider.Usmap;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Versions;
using Fortnite.Core.Ingest;

namespace Fortnite.Ingest;

/// <summary>
/// Envuelve el DefaultFileProvider de CUE4Parse: monta los .pak, entrega la clave AES
/// y carga los mappings .usmap si se indicó uno.
/// </summary>
public sealed class FortniteFileProvider : IDisposable
{
    private readonly IngestOptions _options;

    public FortniteFileProvider(IngestOptions options) => _options = options;

    public DefaultFileProvider Provider { get; private set; } = null!;

    public void Initialize()
    {
        var version = new VersionContainer(EGame.GAME_UE5_LATEST);
        Provider = new DefaultFileProvider(
            _options.PaksDirectory,
            SearchOption.AllDirectories,
            version,
            StringComparer.OrdinalIgnoreCase);
        Provider.Initialize();

        if (!string.IsNullOrWhiteSpace(_options.AesKey))
        {
            Provider.SubmitKey(new FGuid(), new FAesKey(_options.AesKey));
        }

        if (!string.IsNullOrWhiteSpace(_options.MappingsFile))
        {
            Provider.MappingsContainer = new FileUsmapTypeMappingsProvider(_options.MappingsFile!);
        }

        Provider.PostMount();
    }

    public void Dispose() => Provider?.Dispose();
}
