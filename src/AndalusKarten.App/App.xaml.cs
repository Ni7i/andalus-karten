using AndalusKarten.Services;

namespace AndalusKarten;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
        Palette.Apply(Resources, RequestedTheme);
        RequestedThemeChanged += (_, e) => Palette.Apply(Resources, e.RequestedTheme);
    }

    protected override Window CreateWindow(IActivationState? activationState) =>
        new(new AppShell())
        {
            Title = "Andalus Karten",
            // Auf dem Mac: angenehme Startgröße, auf Android ohne Wirkung.
            Width = 900,
            Height = 860,
            MinimumWidth = 420,
            MinimumHeight = 640,
        };
}
