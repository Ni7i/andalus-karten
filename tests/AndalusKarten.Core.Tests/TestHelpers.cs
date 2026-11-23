namespace AndalusKarten.Core.Tests;

internal sealed class FixedClock(DateTimeOffset now) : TimeProvider
{
    public DateTimeOffset Now { get; set; } = now;
    public override DateTimeOffset GetUtcNow() => Now;
    public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
}

internal sealed class InMemoryProgressStore : IProgressStore
{
    public Dictionary<string, CardProgress> Saved { get; private set; } = [];
    public int SaveCount { get; private set; }

    public Task<Dictionary<string, CardProgress>> LoadAsync(CancellationToken ct = default) =>
        Task.FromResult(new Dictionary<string, CardProgress>(Saved));

    public Task SaveAsync(IReadOnlyDictionary<string, CardProgress> progress, CancellationToken ct = default)
    {
        Saved = new Dictionary<string, CardProgress>(progress);
        SaveCount++;
        return Task.CompletedTask;
    }
}

internal static class Words
{
    public static Word Make(string id) => new() { Id = id, Arabic = "كتاب", German = id };
    public static Word[] Many(int n) => Enumerable.Range(1, n).Select(i => Make($"w{i}")).ToArray();
}
