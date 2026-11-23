using System.Text.Json;
using AndalusKarten.Core.Models;

namespace AndalusKarten.Core.Services;

/// <summary>Speichert den Fortschritt als JSON-Datei. Schreibt atomar über eine Temp-Datei.</summary>
public sealed class JsonProgressStore(string filePath) : IProgressStore
{
    private readonly SemaphoreSlim _gate = new(1, 1);

    public async Task<Dictionary<string, CardProgress>> LoadAsync(CancellationToken ct = default)
    {
        if (!File.Exists(filePath))
        {
            return [];
        }

        try
        {
            await using var stream = File.OpenRead(filePath);
            return await JsonSerializer.DeserializeAsync(stream, CoreJsonContext.Default.DictionaryStringCardProgress, ct) ?? [];
        }
        catch (JsonException)
        {
            // Beschädigte Datei: lieber neu anfangen als abstürzen.
            return [];
        }
    }

    public async Task SaveAsync(IReadOnlyDictionary<string, CardProgress> progress, CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct);
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(filePath))!);
            var tmp = filePath + ".tmp";
            await using (var stream = File.Create(tmp))
            {
                await JsonSerializer.SerializeAsync(stream, progress.ToDictionary(), CoreJsonContext.Default.DictionaryStringCardProgress, ct);
            }

            File.Move(tmp, filePath, overwrite: true);
        }
        finally
        {
            _gate.Release();
        }
    }
}
