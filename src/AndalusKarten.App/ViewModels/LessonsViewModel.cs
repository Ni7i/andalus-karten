using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;
using AndalusKarten.Core.Models;
using AndalusKarten.Core.Services;
using AndalusKarten.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AndalusKarten.ViewModels;

/// <summary>Liste der Lektionen einer Art (Buchlektionen oder Essential), durchsuchbar.</summary>
public sealed partial class LessonsViewModel(VocabularyRepository vocabulary, ProgressService progress) : ObservableObject
{
    private List<LessonItem> _all = [];
    private IReadOnlyList<Word> _dueWords = [];

    public LessonKind Kind { get; set; }

    public ObservableCollection<LessonItem> Lessons { get; } = [];

    [ObservableProperty] public partial string Summary { get; set; } = "";
    [ObservableProperty] public partial string SearchText { get; set; } = "";
    [ObservableProperty] public partial int DueCount { get; set; }

    public bool HasDue => DueCount > 0;
    public string DueText => DueCount == 1 ? "1 Karte ist heute fällig" : $"{DueCount} Karten sind heute fällig";

    partial void OnDueCountChanged(int value)
    {
        OnPropertyChanged(nameof(HasDue));
        OnPropertyChanged(nameof(DueText));
    }

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    [RelayCommand]
    private async Task LoadAsync()
    {
        await progress.EnsureLoadedAsync();
        var vocab = await vocabulary.GetAsync();
        var lessons = vocab.OfKind(Kind).ToList();

        _all = lessons.Select(l => new LessonItem(l, progress.StatsFor(l.Words))).ToList();
        var words = lessons.SelectMany(l => l.Words).DistinctBy(w => w.Id).ToList();
        _dueWords = progress.DueWords(words);
        DueCount = _dueWords.Count;
        var stats = progress.StatsFor(words);
        Summary = $"{lessons.Count} Lektionen · {stats.Total} Begriffe · {stats.Mastered} gemeistert";
        ApplyFilter();
    }

    [RelayCommand]
    private Task OpenLessonAsync(LessonItem item) =>
        Shell.Current.GoToAsync(Routes.Lesson, new ShellNavigationQueryParameters { ["lessonId"] = item.Lesson.Id });

    [RelayCommand]
    private Task ReviewDueAsync() =>
        _dueWords.Count == 0
            ? Task.CompletedTask
            : Shell.Current.GoToAsync(Routes.Flashcards,
                new StudyRequest("Wiederholung", _dueWords, _dueWords, $"review-{Kind}").ToQuery());

    private void ApplyFilter()
    {
        var q = Normalize(SearchText);
        Lessons.Clear();
        foreach (var item in _all)
        {
            if (q.Length == 0 || item.Matches(q, Normalize))
            {
                Lessons.Add(item);
            }
        }
    }

    /// <summary>Kleinschreibung, ohne Akzente und ohne arabische Vokalzeichen – damit „kitab“ und „كتاب“ passen.</summary>
    internal static string Normalize(string? s)
    {
        if (string.IsNullOrWhiteSpace(s))
        {
            return "";
        }

        var sb = new StringBuilder(s.Length);
        foreach (var c in s.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD))
        {
            var cat = CharUnicodeInfo.GetUnicodeCategory(c);
            if (cat == UnicodeCategory.NonSpacingMark || c is >= 'ً' and <= 'ْ' or 'ٰ' or 'ـ')
            {
                continue;
            }

            sb.Append(c switch { 'أ' or 'إ' or 'آ' => 'ا', 'ى' => 'ي', 'ة' => 'ه', _ => c });
        }

        return sb.ToString();
    }
}
