using AndalusKarten.Core.Models;
using AndalusKarten.Core.Services;

namespace AndalusKarten.ViewModels;

public sealed class LessonItem(Lesson lesson, LessonStats stats)
{
    public Lesson Lesson { get; } = lesson;
    public LessonStats Stats { get; } = stats;

    public string Badge => Lesson.Number.ToString();
    public string Title => Lesson.Title;
    public string Subtitle => $"{Stats.Total} Wörter · {Stats.Mastered} gemeistert";
    public double Progress => Stats.MasteredRatio;
    public bool HasDue => Stats.Due > 0;
    public string DueText => $"{Stats.Due} fällig";
}
