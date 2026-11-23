using AndalusKarten.Core.Services;
using AndalusKarten.Services;
using AndalusKarten.ViewModels;
using AndalusKarten.Views;

namespace AndalusKarten;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>();

        var services = builder.Services;
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<LeitnerScheduler>();
        services.AddSingleton<IProgressStore>(_ =>
            new JsonProgressStore(Path.Combine(FileSystem.AppDataDirectory, "progress.json")));
        services.AddSingleton<ProgressService>();
        services.AddSingleton<VocabularyRepository>();
        services.AddSingleton<StudySettings>();
        services.AddSingleton<StarService>();

        services.AddTransient<LessonsViewModel>();
        services.AddTransient<LessonViewModel>();
        services.AddTransient<FlashcardsViewModel>();
        services.AddTransient<LearnViewModel>();
        services.AddTransient<MatchViewModel>();
        services.AddTransient<SettingsViewModel>();

        services.AddTransient<LessonsPage>();
        services.AddTransient<EssentialPage>();
        services.AddTransient<LessonPage>();
        services.AddTransient<FlashcardsPage>();
        services.AddTransient<LearnPage>();
        services.AddTransient<MatchPage>();
        services.AddTransient<SettingsPage>();

        return builder.Build();
    }
}
