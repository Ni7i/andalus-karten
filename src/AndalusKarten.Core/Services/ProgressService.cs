using AndalusKarten.Core.Models;

namespace AndalusKarten.Core.Services;

public sealed record LessonStats(int Total, int New, int Learning, int Mastered, int Due)
{
    public double MasteredRatio => Total == 0 ? 0 : (double)Mastered / Total;
    public double SeenRatio => Total == 0 ? 0 : (double)(Total - New) / Total;
}

/// <summary>Hält den Fortschritt im Speicher und persistiert ihn nach jeder Bewertung.</summary>
public sealed class ProgressService(IProgressStore store, LeitnerScheduler scheduler, TimeProvider clock)
{
    private Dictionary<string, CardProgress> _progress = [];
    private bool _loaded;

    public DateOnly Today => DateOnly.FromDateTime(clock.GetLocalNow().DateTime);

    public async Task EnsureLoadedAsync(CancellationToken ct = default)
    {
        if (_loaded)
        {
            return;
        }

        _progress = await store.LoadAsync(ct);
        _loaded = true;
    }

    public CardProgress Get(string wordId) =>
        _progress.TryGetValue(wordId, out var p) ? p : CardProgress.New(Today);

    public async Task<CardProgress> GradeAsync(string wordId, bool knewIt, CancellationToken ct = default)
    {
        var next = scheduler.Grade(Get(wordId), knewIt, Today);
        _progress[wordId] = next;
        await store.SaveAsync(_progress, ct);
        return next;
    }

    /// <summary>Setzt eine Karte auf einen früheren Stand zurück (für „Rückgängig“).</summary>
    public async Task RestoreAsync(string wordId, CardProgress previous, CancellationToken ct = default)
    {
        if (previous is { IsNew: true, TimesCorrect: 0, TimesWrong: 0 })
        {
            _progress.Remove(wordId);
        }
        else
        {
            _progress[wordId] = previous;
        }

        await store.SaveAsync(_progress, ct);
    }

    public LessonStats StatsFor(IEnumerable<Word> words)
    {
        int total = 0, @new = 0, learning = 0, mastered = 0, due = 0;
        var today = Today;
        foreach (var w in words)
        {
            total++;
            var p = Get(w.Id);
            if (p.IsNew) @new++;
            else if (p.IsMastered) mastered++;
            else learning++;

            if (!p.IsNew && p.IsDue(today)) due++;
        }

        return new LessonStats(total, @new, learning, mastered, due);
    }

    /// <summary>Bereits gelernte Karten, deren Wiederholung heute fällig ist.</summary>
    public IReadOnlyList<Word> DueWords(IEnumerable<Word> words)
    {
        var today = Today;
        return words.Where(w => Get(w.Id) is { IsNew: false } p && p.IsDue(today)).ToList();
    }

    public async Task ResetAsync(IEnumerable<string>? wordIds = null, CancellationToken ct = default)
    {
        if (wordIds is null)
        {
            _progress.Clear();
        }
        else
        {
            foreach (var id in wordIds)
            {
                _progress.Remove(id);
            }
        }

        await store.SaveAsync(_progress, ct);
    }
}
