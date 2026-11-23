namespace AndalusKarten.Services;

/// <summary>
/// Farbpalette für Hell- und Dunkelmodus. Die Styles greifen per DynamicResource darauf zu,
/// damit ein Themenwechsel sofort wirkt.
/// </summary>
public static class Palette
{
    private static readonly Dictionary<string, (string Light, string Dark)> Colors = new()
    {
        ["Bg"] = ("#F6F7FB", "#0C1222"),
        ["Surface"] = ("#FFFFFF", "#1B2338"),
        ["SurfaceAlt"] = ("#EEF0F6", "#252F48"),
        ["Ink"] = ("#262C3D", "#F2F4FA"),
        ["Muted"] = ("#646F8A", "#A6AEC4"),
        ["Line"] = ("#E1E4EE", "#2C3650"),
        ["Accent"] = ("#13837D", "#4FC3BD"),
        ["AccentSoft"] = ("#DFF2F0", "#173A3B"),
        ["OnAccent"] = ("#FFFFFF", "#0C1222"),
        ["Known"] = ("#18A26A", "#3DD598"),
        ["KnownSoft"] = ("#E0F5EA", "#143A2C"),
        ["Learning"] = ("#EE7A33", "#FF9F5A"),
        ["LearningSoft"] = ("#FDEBDF", "#432817"),
        ["Wrong"] = ("#E0484A", "#FF6B6D"),
        ["Gold"] = ("#E3A922", "#F4C84A"),
    };

    public static void Apply(ResourceDictionary resources, AppTheme theme)
    {
        var dark = theme == AppTheme.Dark;
        foreach (var (key, (light, darkHex)) in Colors)
        {
            resources[key] = Color.FromArgb(dark ? darkHex : light);
        }
    }

    public static Color Get(string key) =>
        Application.Current?.Resources.TryGetValue(key, out var v) == true && v is Color c ? c : Microsoft.Maui.Graphics.Colors.Gray;
}
