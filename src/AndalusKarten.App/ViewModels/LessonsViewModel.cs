using System.Collections.ObjectModel;
using AndalusKarten.Core.Models;
using AndalusKarten.Core.Services;
using AndalusKarten.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AndalusKarten.ViewModels;

/// <summary>Liste der Lektionen einer Art (normale Lektionen oder Essential).</summary>
public sealed partial class LessonsViewModel(VocabularyRepository vocabulary, ProgressService progress) : ObservableObject
{
    private IReadOnlyList<Word> _dueWords = [];

    public LessonKind Kind { get; set; }

    public ObservableCollection<LessonItem> Lessons { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasDue), nameof(DueButtonText))]
    public partial int DueCount { get; set; }

    [ObservableProperty]
    public partial string Summary { get; set; } = "";

    [ObservableProperty]
    public partial bool IsEmpty { get; set; }

    public bool HasDue => DueCount > 0;

    public string DueButtonText => DueCount == 1 ? "1 Karte wiederholen" : $"{DueCount} Karten wiederholen";

    [RelayCommand]
    private async Task LoadAsync()
    {
        await progress.EnsureLoadedAsync();
        var vocab = await vocabulary.GetAsync();
        var lessons = vocab.OfKind(Kind).ToList();

        Lessons.Clear();
        foreach (var lesson in lessons)
        {
            Lessons.Add(new LessonItem(lesson, progress.StatsFor(lesson.Words)));
        }

        var words = lessons.SelectMany(l => l.Words).ToList();
        var stats = progress.StatsFor(words);
        _dueWords = progress.DueWords(words);
        DueCount = _dueWords.Count;
        IsEmpty = lessons.Count == 0;
        Summary = $"{lessons.Count} Lektionen · {stats.Total} Wörter · {stats.Mastered} gemeistert";
    }

    [RelayCommand]
    private Task OpenLessonAsync(LessonItem item) =>
        Shell.Current.GoToAsync(Routes.Lesson, new ShellNavigationQueryParameters { ["lessonId"] = item.Lesson.Id });

    [RelayCommand]
    private Task ReviewDueAsync() =>
        _dueWords.Count == 0
            ? Task.CompletedTask
            : Shell.Current.GoToAsync(Routes.Study, new ShellNavigationQueryParameters
            {
                [StudyRequest.QueryKey] = new StudyRequest("Wiederholung", _dueWords),
            });
}
