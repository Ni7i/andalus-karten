namespace AndalusKarten.Core.Tests;

public sealed class JsonProgressStoreTests : IDisposable
{
    private readonly string _dir = Directory.CreateTempSubdirectory("andalus-").FullName;
    private string FilePath => Path.Combine(_dir, "sub", "progress.json");

    [Fact]
    public async Task Missing_file_loads_empty()
    {
        Assert.Empty(await new JsonProgressStore(FilePath).LoadAsync());
    }

    [Fact]
    public async Task Round_trips_progress()
    {
        var store = new JsonProgressStore(FilePath);
        var data = new Dictionary<string, CardProgress> { ["L01-001"] = new(3, new DateOnly(2026, 12, 1), 4, 1) };

        await store.SaveAsync(data);
        var loaded = await new JsonProgressStore(FilePath).LoadAsync();

        Assert.Equal(data["L01-001"], loaded["L01-001"]);
        Assert.False(File.Exists(FilePath + ".tmp"));
    }

    [Fact]
    public async Task Corrupt_file_loads_empty()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
        await File.WriteAllTextAsync(FilePath, "{ kaputt");
        Assert.Empty(await new JsonProgressStore(FilePath).LoadAsync());
    }

    public void Dispose() => Directory.Delete(_dir, recursive: true);
}
