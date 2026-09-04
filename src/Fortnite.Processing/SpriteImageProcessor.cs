using System.Security.Cryptography;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Fortnite.Processing;

/// <summary>
/// Normaliza los PNG crudos del ingest: fuerza RGBA, recorta el borde transparente,
/// centra en un lienzo cuadrado y calcula un hash de contenido para deduplicar
/// y para el parámetro ?v= de ruptura de caché. No hace upscale: respeta el tamaño nativo.
/// </summary>
public static class SpriteImageProcessor
{
    /// <summary>Alfa por debajo de esto cuenta como "vacío" para el recorte.</summary>
    private const byte AlphaThreshold = 8;

    public sealed record ProcessedImage(string Id, string FileName, string Hash, int Width, int Height, bool Changed);

    /// <summary>Lienzo cuadrado uniforme de salida (tamaño nativo máximo de estos iconos). No hay upscale.</summary>
    public const int CanvasSize = 128;

    public static IReadOnlyList<ProcessedImage> Run(string stagingTexturesDir, string outputDir, Action<string> log)
    {
        Directory.CreateDirectory(outputDir);
        var results = new List<ProcessedImage>();
        var changed = 0;

        foreach (var src in Directory.EnumerateFiles(stagingTexturesDir, "*.png").OrderBy(p => p, StringComparer.OrdinalIgnoreCase))
        {
            var id = Path.GetFileNameWithoutExtension(src);
            try
            {
                using var image = Image.Load<Rgba32>(src);
                TrimTransparentBorder(image);
                PadToCanvas(image);

                using var ms = new MemoryStream();
                image.SaveAsPng(ms);
                var bytes = ms.ToArray();
                var hash = Convert.ToHexString(SHA256.HashData(bytes))[..16].ToLowerInvariant();

                var outPath = Path.Combine(outputDir, id + ".png");
                var isChanged = !File.Exists(outPath) || new FileInfo(outPath).Length != bytes.Length || !HashMatches(outPath, hash);
                if (isChanged)
                {
                    File.WriteAllBytes(outPath, bytes);
                    changed++;
                }

                results.Add(new ProcessedImage(id, id + ".png", hash, image.Width, image.Height, isChanged));
            }
            catch (Exception ex)
            {
                log($"  ERROR procesando {id}: {ex.Message}");
            }
        }

        log($"Imágenes procesadas: {results.Count} ({changed} nuevas/cambiadas) -> {outputDir}");

        var dupes = results.GroupBy(r => r.Hash).Where(g => g.Count() > 1).ToArray();
        foreach (var g in dupes)
        {
            log($"  duplicadas (mismo contenido): {string.Join(", ", g.Select(x => x.Id))}");
        }

        return results;
    }

    private static void TrimTransparentBorder(Image<Rgba32> image)
    {
        int top = 0, bottom = image.Height - 1, left = 0, right = image.Width - 1;

        bool RowHasPixels(int y)
        {
            for (var x = 0; x < image.Width; x++)
            {
                if (image[x, y].A > AlphaThreshold)
                {
                    return true;
                }
            }

            return false;
        }

        bool ColHasPixels(int x)
        {
            for (var y = 0; y < image.Height; y++)
            {
                if (image[x, y].A > AlphaThreshold)
                {
                    return true;
                }
            }

            return false;
        }

        while (top < bottom && !RowHasPixels(top))
        {
            top++;
        }

        while (bottom > top && !RowHasPixels(bottom))
        {
            bottom--;
        }

        while (left < right && !ColHasPixels(left))
        {
            left++;
        }

        while (right > left && !ColHasPixels(right))
        {
            right--;
        }

        var w = right - left + 1;
        var h = bottom - top + 1;
        if (w > 0 && h > 0 && (w != image.Width || h != image.Height))
        {
            image.Mutate(c => c.Crop(new Rectangle(left, top, w, h)));
        }
    }

    /// <summary>
    /// Centra el contenido en un lienzo cuadrado uniforme (<see cref="CanvasSize"/>).
    /// Nunca escala: si algún icono superara el lienzo, se agranda el lienzo a ese tamaño.
    /// </summary>
    private static void PadToCanvas(Image<Rgba32> image)
    {
        var side = Math.Max(CanvasSize, Math.Max(image.Width, image.Height));
        if (image.Width == side && image.Height == side)
        {
            return;
        }

        image.Mutate(c => c.Resize(new ResizeOptions
        {
            Size = new Size(side, side),
            Mode = ResizeMode.BoxPad,
            Position = AnchorPositionMode.Center,
            PadColor = Color.Transparent,
            Sampler = KnownResamplers.NearestNeighbor,
        }));
    }

    private static bool HashMatches(string path, string expectedHash)
    {
        try
        {
            var h = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)))[..16].ToLowerInvariant();
            return h == expectedHash;
        }
        catch
        {
            return false;
        }
    }
}
