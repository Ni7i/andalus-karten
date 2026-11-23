using AndalusKarten.Core.Models;

namespace AndalusKarten.Services;

/// <summary>Wird per Shell-Navigation an die Lernmodi übergeben.</summary>
/// <param name="Words">Karten dieser Runde.</param>
/// <param name="Pool">Alle Karten der Lektion – Quelle für falsche Antwortmöglichkeiten.</param>
/// <param name="Key">Schlüssel für Bestzeiten (z. B. Lektions-ID).</param>
public sealed record StudyRequest(string Title, IReadOnlyList<Word> Words, IReadOnlyList<Word> Pool, string Key)
{
    public const string QueryKey = "request";

    public ShellNavigationQueryParameters ToQuery() => new() { [QueryKey] = this };
}
