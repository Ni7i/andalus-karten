using System.Text.Json;
using AndalusKarten.Core.Models;

namespace AndalusKarten.Core.Services;

/// <summary>
/// Liest das Vokabel-JSON-Format:
/// <code>
/// { "lessons": [ { "id": "L01", "number": 1, "title": "…", "essential": false,
///                  "words": [ { "id": "L01-001", "ar": "…", "de": "…", "tr": "…", "note": "…" } ] } ] }
/// </code>
/// </summary>
public static class VocabularyParser
{
    public static async Task<Vocabulary> ParseAsync(Stream json, CancellationToken ct = default)
    {
        var dto = await JsonSerializer.DeserializeAsync(json, CoreJsonContext.Default.VocabularyDto, ct)
                  ?? throw new FormatException("Leere Vokabeldatei.");
        return Map(dto);
    }

    public static Vocabulary Parse(string json)
    {
        var dto = JsonSerializer.Deserialize(json, CoreJsonContext.Default.VocabularyDto)
                  ?? throw new FormatException("Leere Vokabeldatei.");
        return Map(dto);
    }

    private static Vocabulary Map(VocabularyDto dto)
    {
        var lessons = new List<Lesson>();
        var seenLessonIds = new HashSet<string>(StringComparer.Ordinal);
        var seenWordIds = new HashSet<string>(StringComparer.Ordinal);

        foreach (var (l, li) in (dto.Lessons ?? []).Select((l, i) => (l, i)))
        {
            var kind = l.Essential ? LessonKind.Essential : LessonKind.Lesson;
            var number = l.Number ?? li + 1;
            var lessonId = string.IsNullOrWhiteSpace(l.Id)
                ? $"{(kind == LessonKind.Essential ? "E" : "L")}{number:D2}"
                : l.Id.Trim();

            if (!seenLessonIds.Add(lessonId))
            {
                throw new FormatException($"Doppelte Lektions-ID „{lessonId}“.");
            }

            var words = new List<Word>();
            foreach (var (w, wi) in (l.Words ?? []).Select((w, i) => (w, i)))
            {
                if (string.IsNullOrWhiteSpace(w.Ar) || string.IsNullOrWhiteSpace(w.De))
                {
                    continue; // unvollständige Zeilen still überspringen
                }

                var wordId = string.IsNullOrWhiteSpace(w.Id) ? $"{lessonId}-{wi + 1:D3}" : w.Id.Trim();
                if (!seenWordIds.Add(wordId))
                {
                    throw new FormatException($"Doppelte Wort-ID „{wordId}“.");
                }

                words.Add(new Word
                {
                    Id = wordId,
                    Arabic = w.Ar.Trim(),
                    German = w.De.Trim(),
                    Transliteration = NullIfBlank(w.Tr),
                    Note = NullIfBlank(w.Note),
                });
            }

            lessons.Add(new Lesson
            {
                Id = lessonId,
                Number = number,
                Title = string.IsNullOrWhiteSpace(l.Title) ? "Ohne Titel" : l.Title.Trim(),
                Kind = kind,
                Words = words,
            });
        }

        return new Vocabulary(lessons);
    }

    private static string? NullIfBlank(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
