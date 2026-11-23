namespace AndalusKarten.Services;

/// <summary>Markierte (★) Karten, gespeichert in den Preferences.</summary>
public sealed class StarService
{
    private const string Key = "starred";
    private readonly HashSet<string> _ids;

    public StarService() =>
        _ids = new HashSet<string>(
            Preferences.Default.Get(Key, "").Split('|', StringSplitOptions.RemoveEmptyEntries),
            StringComparer.Ordinal);

    public bool IsStarred(string id) => _ids.Contains(id);

    public bool Toggle(string id)
    {
        var starred = _ids.Add(id) || !_ids.Remove(id);
        Preferences.Default.Set(Key, string.Join('|', _ids));
        return starred;
    }
}
