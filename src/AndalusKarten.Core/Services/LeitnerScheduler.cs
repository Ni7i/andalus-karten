using AndalusKarten.Core.Models;

namespace AndalusKarten.Core.Services;

/// <summary>
/// Klassisches Leitner-System: richtig → eine Box höher, falsch → zurück in Box 1.
/// Höhere Boxen werden seltener wiederholt.
/// </summary>
public sealed class LeitnerScheduler
{
    // Index = Box. Box 0 (neu) wird nie als Ziel vergeben.
    private static readonly int[] IntervalDays = [0, 1, 2, 4, 8, 16];

    public CardProgress Grade(CardProgress current, bool knewIt, DateOnly today)
    {
        if (!knewIt)
        {
            return current with { Box = 1, DueOn = today, TimesWrong = current.TimesWrong + 1 };
        }

        var box = Math.Min(current.Box + 1, CardProgress.MaxBox);
        return current with
        {
            Box = box,
            DueOn = today.AddDays(IntervalDays[box]),
            TimesCorrect = current.TimesCorrect + 1,
        };
    }
}
