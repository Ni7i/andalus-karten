using System.Collections.ObjectModel;
using AndalusKarten.Core.Models;
using AndalusKarten.Core.Services;
using AndalusKarten.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AndalusKarten.ViewModels;

public sealed partial class WordRow(Word word, CardProgress progress, bool starred) : ObservableObject
{
    public Word Word { get; } = word;
    public string Arabic => Word.Arabic;
    public string German => Word.German;
    public string? Detail => string.Join(" · ", new[] { Word.Transliteration, Word.Note }.Where(s => !string.IsNullOrWhiteSpace(s)));
    public bool HasDetail => !string.IsNullOrEmpty(Detail);

    public string Status => progress.IsNew ? "Neu" : progress.IsMastered ? "Gemeistert" : "Am Lernen";
    public string StatusColorKey => progress.IsNew ? "Muted" : progress.IsMastered ? "Known" : "Learning";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StarGlyph))]
    public partial bool IsStarred { get; set; } = starred;

    public string StarGlyph => IsStarred ? "★" : "☆";
}

/// <summary>Übersicht einer Lektion: Vorschaukarte, Lernmodi und Begriffsliste.</summary>
public sealed partial class LessonViewModel(VocabularyRepository vocabulary, ProgressService progress, StarService stars)
    : ObservableObject, IQueryAttributable
{
    private string? _lessonId;
    private Lesson? _lesson;
    private int _previewIndex;

    public ObservableCollection<WordRow> Words { get; } = [];

    [ObservableProperty] public partial string Title { get; set; } = "";
    [ObservableProperty] public partial string Subtitle { get; set; } = "";
    [ObservableProperty] public partial string PreviewText { get; set; } = "";
    [ObservableProperty] public partial bool PreviewIsArabic { get; set; } = true;
    [ObservableProperty] public partial string PreviewCounter { get; set; } = "";
    [ObservableProperty] public partial bool OnlyStarred { get; set; }
    [ObservableProperty] public partial int StarredCount { get; set; }
    [ObservableProperty] public partial int KnownCount { get; set; }
    [ObservableProperty] public partial int LearningCount { get; set; }
    [ObservableProperty] public partial int NewCount { get; set; }
    [ObservableProperty] public partial int DueCount { get; set; }

    public bool HasStarred => StarredCount > 0;
    public bool HasDue => DueCount > 0;
    public string DueText => $"{DueCount} fällige Karten wiederholen";
    public string StarredText => $"Nur markierte lernen ({StarredCount})";
    public double PreviewFontSize => PreviewIsArabic ? 40 : 24;
    public FlowDirection PreviewFlow => PreviewIsArabic ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;

    partial void OnStarredCountChanged(int value)
    {
        OnPropertyChanged(nameof(HasStarred));
        OnPropertyChanged(nameof(StarredText));
    }

    partial void OnDueCountChanged(int value)
    {
        OnPropertyChanged(nameof(HasDue));
        OnPropertyChanged(nameof(DueText));
    }

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

        Title = _lesson.Kind == LessonKind.Essential ? $"Essential {_lesson.Number}" : $"Lektion {_lesson.Number}";
        Subtitle = $"{_lesson.Words.Count} Begriffe · {_lesson.Title}";

        var stats = progress.StatsFor(_lesson.Words);
        KnownCount = stats.Mastered;
        LearningCount = stats.Learning;
        NewCount = stats.New;
        DueCount = stats.Due;

        Words.Clear();
        foreach (var w in _lesson.Words)
        {
            Words.Add(new WordRow(w, progress.Get(w.Id), stars.IsStarred(w.Id)));
        }

        StarredCount = Words.Count(r => r.IsStarred);
        _previewIndex = Math.Clamp(_previewIndex, 0, Math.Max(0, Words.Count - 1));
        ShowPreview(arabic: true);
    }

    [RelayCommand]
    private void FlipPreview() => ShowPreview(!PreviewIsArabic);

    [RelayCommand]
    private void NextPreview() => MovePreview(1);

    [RelayCommand]
    private void PreviousPreview() => MovePreview(-1);

    [RelayCommand]
    private void ToggleStar(WordRow row)
    {
        row.IsStarred = stars.Toggle(row.Word.Id);
        StarredCount = Words.Count(r => r.IsStarred);
    }

    [RelayCommand]
    private Task FlashcardsAsync() => GoAsync(Routes.Flashcards, SelectedWords());

    [RelayCommand]
    private Task LearnAsync() => GoAsync(Routes.Learn, SelectedWords());

    [RelayCommand]
    private Task MatchAsync() => GoAsync(Routes.Match, SelectedWords());

    [RelayCommand]
    private Task ReviewDueAsync() =>
        GoAsync(Routes.Flashcards, progress.DueWords(_lesson?.Words ?? []));

    [RelayCommand]
    private async Task ResetAsync()
    {
        if (_lesson is null ||
            !await Dialogs.ConfirmAsync("Fortschritt zurücksetzen?",
                $"Alle Karten aus {Title} gelten danach wieder als neu.", "Zurücksetzen"))
        {
            return;
        }

        await progress.ResetAsync(_lesson.Words.Select(w => w.Id));
        await LoadAsync();
    }

    private IReadOnlyList<Word> SelectedWords() =>
        OnlyStarred && HasStarred
            ? Words.Where(r => r.IsStarred).Select(r => r.Word).ToList()
            : _lesson?.Words ?? [];

    private Task GoAsync(string route, IReadOnlyList<Word> words) =>
        words.Count == 0 || _lesson is null
            ? Task.CompletedTask
            : Shell.Current.GoToAsync(route, new StudyRequest(Title, words, _lesson.Words, _lesson.Id).ToQuery());

    private void MovePreview(int delta)
    {
        if (Words.Count == 0)
        {
            return;
        }

        _previewIndex = (_previewIndex + delta + Words.Count) % Words.Count;
        ShowPreview(arabic: true);
    }

    private void ShowPreview(bool arabic)
    {
        if (Words.Count == 0)
        {
            return;
        }

        var w = Words[_previewIndex].Word;
        PreviewIsArabic = arabic;
        PreviewText = arabic ? w.Arabic : w.German;
        PreviewCounter = $"{_previewIndex + 1} / {Words.Count}";
        OnPropertyChanged(nameof(PreviewFontSize));
        OnPropertyChanged(nameof(PreviewFlow));
    }
}
