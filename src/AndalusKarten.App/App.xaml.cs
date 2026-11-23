namespace AndalusKarten;

public partial class App : Application
{
    public App() => InitializeComponent();

    protected override Window CreateWindow(IActivationState? activationState) =>
        new(new AppShell())
        {
            Title = "Andalus Karten",
            // Auf dem Mac: angenehme Startgröße, auf Android ohne Wirkung.
            Width = 520,
            Height = 820,
            MinimumWidth = 380,
            MinimumHeight = 560,
        };
}
