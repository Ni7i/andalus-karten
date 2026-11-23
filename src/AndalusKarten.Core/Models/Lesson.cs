namespace AndalusKarten.Core.Models;

public enum LessonKind
{
    Lesson,
    Essential,
}

public sealed record Lesson
{
    public required string Id { get; init; }
    public required int Number { get; init; }
    public required string Title { get; init; }
    public LessonKind Kind { get; init; } = LessonKind.Lesson;
    public IReadOnlyList<Word> Words { get; init; } = [];

    public string DisplayName => Kind == LessonKind.Essential
        ? $"Essential {Number} · {Title}"
        : $"Lektion {Number} · {Title}";
}
