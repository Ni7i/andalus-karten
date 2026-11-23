using AndalusKarten.Core.Models;

namespace AndalusKarten.Core.Services;

public sealed record ChoiceQuestion(Word Word, IReadOnlyList<Word> Options, int CorrectIndex);

/// <summary>Erzeugt Multiple-Choice-Fragen und Zuordnungsrunden.</summary>
public static class QuizBuilder
{
    /// <summary>
    /// Eine Frage mit bis zu <paramref name="optionCount"/> Antworten. Ablenker haben eine andere
    /// Bedeutung und einen anderen arabischen Text als das gesuchte Wort.
    /// </summary>
    public static ChoiceQuestion Choice(Word word, IReadOnlyList<Word> pool, Random random, int optionCount = 4)
    {
        var distractors = pool
            .Where(w => w.Id != word.Id
                        && !string.Equals(w.German, word.German, StringComparison.OrdinalIgnoreCase)
                        && w.Arabic != word.Arabic)
            .DistinctBy(w => w.German.ToLowerInvariant())
            .OrderBy(_ => random.Next())
            .Take(optionCount - 1);

        var options = distractors.Append(word).OrderBy(_ => random.Next()).ToList();
        return new ChoiceQuestion(word, options, options.FindIndex(o => o.Id == word.Id));
    }

    /// <summary>Wählt bis zu <paramref name="pairs"/> Wörter mit eindeutigen Bedeutungen für eine Zuordnungsrunde.</summary>
    public static IReadOnlyList<Word> MatchSet(IReadOnlyList<Word> pool, Random random, int pairs = 6) =>
        pool.DistinctBy(w => w.German.ToLowerInvariant())
            .DistinctBy(w => w.Arabic)
            .OrderBy(_ => random.Next())
            .Take(pairs)
            .ToList();
}
