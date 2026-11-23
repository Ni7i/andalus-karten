using AndalusKarten.Core.Models;
using AndalusKarten.Core.Services;

namespace AndalusKarten.ViewModels;

public sealed class LessonItem(Lesson lesson, LessonStats stats)
{
    public Lesson Lesson { get; } = lesson;
    public LessonStats Stats { get; } = stats;

    public string Name => Lesson.Kind == LessonKind.Essential ? $"Essential {Lesson.Number}" : $"Lektion {Lesson.Number}";
    public string Topic => Lesson.Title;
    public string TermCount => $"{Stats.Total} Begriffe";
    public double Progress => Stats.MasteredRatio;
    public string ProgressText => $"{Stats.MasteredRatio:P0}";
    public bool HasDue => Stats.Due > 0;
    public string DueText => $"{Stats.Due} fällig";

    public bool Matches(string query, Func<string, string> normalize) =>
        normalize(Name).Contains(query) || normalize(Topic).Contains(query) ||
        Lesson.Words.Any(w => normalize(w.German).Contains(query) || normalize(w.Arabic).Contains(query)
                              || normalize(w.Transliteration ?? "").Contains(query));
}
