using AndalusKarten.Core.Models;
using AndalusKarten.Core.Services;

namespace AndalusKarten.Services;

/// <summary>
/// Liefert die Vokabeln: eine importierte Datei (falls vorhanden) hat Vorrang vor der
/// mit der App ausgelieferten <c>vocabulary.json</c>.
/// </summary>
public sealed class VocabularyRepository
{
    private const string BundledFile = "vocabulary.json";
    private readonly string _importPath = Path.Combine(FileSystem.AppDataDirectory, "vocabulary.imported.json");
    private readonly SemaphoreSlim _gate = new(1, 1);
    private Vocabulary? _cache;

    public event EventHandler? Changed;

    public bool IsImported => File.Exists(_importPath);

    public DateTime? ImportedAt => IsImported ? File.GetLastWriteTime(_importPath) : null;

    public async Task<Vocabulary> GetAsync()
    {
        if (_cache is not null)
        {
            return _cache;
        }

        await _gate.WaitAsync();
        try
        {
            return _cache ??= await LoadAsync();
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>Prüft die Datei vollständig, bevor sie die aktuelle Liste ersetzt.</summary>
    public async Task<Vocabulary> ImportAsync(Stream source)
    {
        using var buffer = new MemoryStream();
        await source.CopyToAsync(buffer);

        buffer.Position = 0;
        var vocabulary = await VocabularyParser.ParseAsync(buffer);
        if (!vocabulary.AllWords.Any())
        {
            throw new FormatException("Die Datei enthält keine Wörter.");
        }

        await File.WriteAllBytesAsync(_importPath, buffer.ToArray());
        _cache = vocabulary;
        Changed?.Invoke(this, EventArgs.Empty);
        return vocabulary;
    }

    public Task ResetToBundledAsync()
    {
        if (IsImported)
        {
            File.Delete(_importPath);
        }

        _cache = null;
        Changed?.Invoke(this, EventArgs.Empty);
        return Task.CompletedTask;
    }

    private async Task<Vocabulary> LoadAsync()
    {
        if (IsImported)
        {
            try
            {
                await using var imported = File.OpenRead(_importPath);
                return await VocabularyParser.ParseAsync(imported);
            }
            catch (Exception ex) when (ex is FormatException or System.Text.Json.JsonException or IOException)
            {
                // Fällt auf die mitgelieferte Liste zurück.
            }
        }

        await using var bundled = await FileSystem.OpenAppPackageFileAsync(BundledFile);
        return await VocabularyParser.ParseAsync(bundled);
    }
}
