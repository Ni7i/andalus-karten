namespace AndalusKarten.Core.Tests;

public class ProgressServiceTests
{
    private readonly FixedClock _clock = new(new DateTimeOffset(2026, 11, 23, 10, 0, 0, TimeSpan.Zero));
    private readonly InMemoryProgressStore _store = new();

    private ProgressService CreateSut() => new(_store, new LeitnerScheduler(), _clock);

    [Fact]
    public async Task Grade_persists_and_affects_stats()
    {
        var sut = CreateSut();
        await sut.EnsureLoadedAsync();
        var words = Words.Many(3);

        await sut.GradeAsync("w1", knewIt: true);
        await sut.GradeAsync("w2", knewIt: false);

        var stats = sut.StatsFor(words);
        Assert.Equal(new LessonStats(Total: 3, New: 1, Learning: 2, Mastered: 0, Due: 1), stats);
        Assert.Equal(2, _store.SaveCount);
        Assert.Equal(2, _store.Saved.Count);
    }

    [Fact]
    public async Task Card_becomes_due_after_interval()
    {
        var sut = CreateSut();
        await sut.EnsureLoadedAsync();
        var words = Words.Many(1);

        await sut.GradeAsync("w1", knewIt: true); // Box 1 → morgen fällig
        Assert.Empty(sut.DueWords(words));

        _clock.Now = _clock.Now.AddDays(1);
        Assert.Single(sut.DueWords(words));
    }

    [Fact]
    public async Task Progress_survives_reload()
    {
        var first = CreateSut();
        await first.EnsureLoadedAsync();
        await first.GradeAsync("w1", knewIt: true);

        var second = CreateSut();
        await second.EnsureLoadedAsync();
        Assert.Equal(1, second.Get("w1").Box);
    }

    [Fact]
    public async Task Reset_selected_words_only()
    {
        var sut = CreateSut();
        await sut.EnsureLoadedAsync();
        await sut.GradeAsync("w1", true);
        await sut.GradeAsync("w2", true);

        await sut.ResetAsync(["w1"]);

        Assert.True(sut.Get("w1").IsNew);
        Assert.False(sut.Get("w2").IsNew);
    }

    [Fact]
    public async Task Restore_undoes_a_grade()
    {
        var sut = CreateSut();
        await sut.EnsureLoadedAsync();
        var before = sut.Get("w1");

        await sut.GradeAsync("w1", true);
        await sut.RestoreAsync("w1", before);

        Assert.True(sut.Get("w1").IsNew);
        Assert.Empty(_store.Saved);
    }
}
