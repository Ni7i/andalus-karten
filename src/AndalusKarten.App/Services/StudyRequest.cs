using AndalusKarten.Core.Models;

namespace AndalusKarten.Services;

/// <summary>Wird per Shell-Navigation an die Lernseite übergeben.</summary>
public sealed record StudyRequest(string Title, IReadOnlyList<Word> Words)
{
    public const string QueryKey = "request";
}
