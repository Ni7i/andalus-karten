namespace AndalusKarten.Services;

public enum CardDirection
{
    ArabicToGerman = 0,
    GermanToArabic = 1,
    Mixed = 2,
}

/// <summary>Kleine Benutzereinstellungen, gespeichert über <see cref="Preferences"/>.</summary>
public sealed class StudySettings
{
    public CardDirection Direction
    {
        get => (CardDirection)Preferences.Default.Get(nameof(Direction), (int)CardDirection.ArabicToGerman);
        set => Preferences.Default.Set(nameof(Direction), (int)value);
    }

    public bool ShowTransliteration
    {
        get => Preferences.Default.Get(nameof(ShowTransliteration), true);
        set => Preferences.Default.Set(nameof(ShowTransliteration), value);
    }
}
