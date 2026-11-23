using AndalusKarten.Views;

namespace AndalusKarten;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        Routing.RegisterRoute(Routes.Lesson, typeof(LessonPage));
        Routing.RegisterRoute(Routes.Flashcards, typeof(FlashcardsPage));
        Routing.RegisterRoute(Routes.Learn, typeof(LearnPage));
        Routing.RegisterRoute(Routes.Match, typeof(MatchPage));
    }
}

public static class Routes
{
    public const string Lesson = "lesson";
    public const string Flashcards = "flashcards";
    public const string Learn = "learn";
    public const string Match = "match";
}
