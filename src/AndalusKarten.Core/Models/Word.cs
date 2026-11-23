namespace AndalusKarten.Core.Models;

/// <summary>Eine Vokabel. <see cref="Id"/> ist stabil und dient als Schlüssel für den Lernfortschritt.</summary>
public sealed record Word
{
    public required string Id { get; init; }

    /// <summary>Arabisch (gern mit Harakat).</summary>
    public required string Arabic { get; init; }

    /// <summary>Deutsche Bedeutung.</summary>
    public required string German { get; init; }

    /// <summary>Optionale Umschrift, z. B. „marḥaban“.</summary>
    public string? Transliteration { get; init; }

    /// <summary>Optionale Notiz: Plural, Wurzel, Beispiel …</summary>
    public string? Note { get; init; }
}
