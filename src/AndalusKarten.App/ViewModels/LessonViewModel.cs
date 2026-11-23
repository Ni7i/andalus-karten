using System.Collections.ObjectModel;
using AndalusKarten.Core.Models;
using AndalusKarten.Core.Services;
using AndalusKarten.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AndalusKarten.ViewModels;

public sealed class WordRow(Word word, CardProgress progress)
{
    public string Arabic => word.Arabic;
    public string German => word.German;
    public string? Transliteration => word.Transliteration;
    public bool HasTransliteration => !string.IsNullOrEmpty(word.Transliteration);

    /// <summary>Fünf Punkte für die Leitner-Box: ●●●○○</summary>
    public string Level => progress.IsNew
        ? "neu"
        : new string('●', progress.Box) + new string('○', CardProgress.MaxBox - progress.Box);
}

public sealed partial class LessonViewModel(VocabularyRepository vocabulary, ProgressService progress)
    : ObservableObject, IQueryAttributable
{
    private string? _lessonId;
    private Lesson? _lesson;

    public ObservableCollection<WordRow> Words { get; } = [];

    [ObservableProperty]
    public partial string Title { get; set; } = "";

    [ObservableProperty]
    public partial string Summary { get; set; } = "";

    [ObservableProperty]
    public partial double Progress { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasWeak), nameof(WeakButtonText))]
    public partial int WeakCount { get; set; }

    public bool HasWeak => WeakCount > 0;

    public string WeakButtonText => $"Nur unsichere ({WeakCount})";

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("lessonId", out var id))
        {
            _lessonId = id as string;
        }
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        await progress.EnsureLoadedAsync();
        var vocab = await vocabulary.GetAsync();
        _lesson = _lessonId is null ? null : vocab.FindLesson(_lessonId);
        if (_lesson is null)
        {
            await Shell.Current.GoToAsync("..");
            return;
        }

        Title = _lesson.DisplayName;
        var stats = progress.StatsFor(_lesson.Words);
        Summary = $"{stats.Total} Wörter · {stats.New} neu · {stats.Learning} im Lernen · {stats.Mastered} gemeistert";
        Progress = stats.MasteredRatio;
        WeakCount = stats.Total - stats.Mastered;

        Words.Clear();
        foreach (var word in _lesson.Words)
        {
            Words.Add(new WordRow(word, progress.Get(word.Id)));
        }
    }

    [RelayCommand]
    private Task StudyAllAsync() => StartAsync(_lesson?.Words ?? []);

    [RelayCommand]
    private Task StudyWeakAsync() =>
        StartAsync(_lesson?.Words.Where(w => !progress.Get(w.Id).IsMastered).ToList() ?? []);

    [RelayCommand]
    private async Task ResetAsync()
    {
        if (_lesson is null ||
            !await Dialogs.ConfirmAsync("Fortschritt zurücksetzen?",
                $"Alle Karten aus „{_lesson.Title}“ gelten danach wieder als neu.", "Zurücksetzen"))
        {
            return;
        }

        await progress.ResetAsync(_lesson.Words.Select(w => w.Id));
        await LoadAsync();
    }

    private Task StartAsync(IReadOnlyList<Word> words) =>
        words.Count == 0 || _lesson is null
            ? Task.CompletedTask
            : Shell.Current.GoToAsync(Routes.Study, new ShellNavigationQueryParameters
            {
                [StudyRequest.QueryKey] = new StudyRequest(_lesson.Title, words),
            });
}
