using AndalusKarten.Views;

namespace AndalusKarten;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        Routing.RegisterRoute(Routes.Lesson, typeof(LessonPage));
        Routing.RegisterRoute(Routes.Study, typeof(StudyPage));
    }
}

public static class Routes
{
    public const string Lesson = "lesson";
    public const string Study = "study";
}
