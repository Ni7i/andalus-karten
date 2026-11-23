namespace AndalusKarten.Core.Models;

public sealed record Vocabulary(IReadOnlyList<Lesson> Lessons)
{
    public static Vocabulary Empty { get; } = new([]);

    public IEnumerable<Lesson> OfKind(LessonKind kind) =>
        Lessons.Where(l => l.Kind == kind).OrderBy(l => l.Number);

    public IEnumerable<Word> AllWords => Lessons.SelectMany(l => l.Words);

    public Lesson? FindLesson(string id) => Lessons.FirstOrDefault(l => l.Id == id);
}
