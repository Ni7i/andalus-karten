namespace AndalusKarten.Core.Models;

/// <summary>Leitner-Zustand einer Karte. Box 0 = neu, 1..<see cref="MaxBox"/> = gelernt.</summary>
public sealed record CardProgress(int Box, DateOnly DueOn, int TimesCorrect, int TimesWrong)
{
    public const int MaxBox = 5;

    public static CardProgress New(DateOnly today) => new(0, today, 0, 0);

    public bool IsNew => Box == 0;
    public bool IsMastered => Box >= MaxBox;
    public bool IsDue(DateOnly today) => DueOn <= today;
}
