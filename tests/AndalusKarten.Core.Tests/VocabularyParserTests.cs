namespace AndalusKarten.Core.Tests;

public class VocabularyParserTests
{
    [Fact]
    public void Parses_lessons_and_generates_missing_ids()
    {
        const string json = """
        {
          // Kommentare sind erlaubt
          "lessons": [
            { "number": 1, "title": "Begrüßung", "words": [
                { "ar": "مَرْحَبًا", "de": "Hallo", "tr": "marḥaban" },
                { "ar": "  ", "de": "wird übersprungen" },
                { "ar": "شُكْرًا", "de": "Danke", "note": "" },
            ] },
            { "id": "E-basics", "number": 1, "title": "Grundwortschatz", "essential": true, "words": [
                { "id": "x1", "ar": "بَيْت", "de": "Haus" }
            ] }
          ]
        }
        """;

        var vocab = VocabularyParser.Parse(json);

        var lesson = Assert.Single(vocab.OfKind(LessonKind.Lesson));
        Assert.Equal("L01", lesson.Id);
        Assert.Equal("Lektion 1 · Begrüßung", lesson.DisplayName);
        Assert.Equal(["L01-001", "L01-003"], lesson.Words.Select(w => w.Id));
        Assert.Null(lesson.Words[1].Note);
        Assert.Equal("marḥaban", lesson.Words[0].Transliteration);

        var essential = Assert.Single(vocab.OfKind(LessonKind.Essential));
        Assert.Equal("E-basics", essential.Id);
        Assert.Equal("x1", essential.Words[0].Id);
        Assert.Equal(3, vocab.AllWords.Count());
    }

    [Fact]
    public void Duplicate_word_ids_are_rejected()
    {
        const string json = """
        { "lessons": [ { "id": "A", "words": [ { "id": "1", "ar": "a", "de": "a" }, { "id": "1", "ar": "b", "de": "b" } ] } ] }
        """;

        Assert.Throws<FormatException>(() => VocabularyParser.Parse(json));
    }

    [Fact]
    public void Same_word_id_in_different_lessons_is_allowed()
    {
        const string json = """
        { "lessons": [
            { "id": "L01", "words": [ { "id": "L01-001", "ar": "a", "de": "a" } ] },
            { "id": "E01", "essential": true, "words": [ { "id": "L01-001", "ar": "a", "de": "a" } ] } ] }
        """;

        var vocab = VocabularyParser.Parse(json);
        Assert.Equal(2, vocab.Lessons.Count);
        Assert.Single(vocab.AllWords.DistinctBy(w => w.Id));
    }

    [Fact]
    public async Task Bundled_vocabulary_file_is_valid()
    {
        await using var stream = File.OpenRead(Path.Combine(AppContext.BaseDirectory, "vocabulary.json"));
        var vocab = await VocabularyParser.ParseAsync(stream);

        Assert.NotEmpty(vocab.Lessons);
        Assert.All(vocab.Lessons, l => Assert.NotEmpty(l.Words));
    }
}
