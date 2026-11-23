using AndalusKarten.Core.Models;

namespace AndalusKarten.Core.Services;

/// <summary>
/// Eine Karteikarten-Runde: jede Karte wird nach links („lerne ich noch“) oder rechts („gewusst“)
/// gewischt. Der letzte Wisch lässt sich rückgängig machen. Am Ende kann man mit den
/// noch nicht gewussten Karten weitermachen.
/// </summary>
public sealed class FlashcardRound
{
    private readonly List<Word> _cards;
    private readonly Stack<(Word Word, bool Knew)> _history = new();

    public FlashcardRound(IEnumerable<Word> words, bool shuffle, Random? random = null)
    {
        _cards = words.ToList();
        if (shuffle)
        {
            var arr = _cards.ToArray();
            (random ?? Random.Shared).Shuffle(arr);
            _cards = [.. arr];
        }
    }

    public int Total => _cards.Count;
    public int Index => _history.Count;
    public Word? Current => Index < Total ? _cards[Index] : null;
    public bool IsFinished => Index >= Total;
    public bool CanUndo => _history.Count > 0;

    public int KnownCount => _history.Count(h => h.Knew);
    public int LearningCount => _history.Count(h => !h.Knew);

    public IReadOnlyList<Word> StillLearning => _history.Where(h => !h.Knew).Select(h => h.Word).Reverse().ToList();

    public void Answer(bool knew)
    {
        var word = Current ?? throw new InvalidOperationException("Die Runde ist beendet.");
        _history.Push((word, knew));
    }

    /// <summary>Macht den letzten Wisch rückgängig und liefert die betroffene Karte.</summary>
    public (Word Word, bool Knew)? Undo() => _history.Count > 0 ? _history.Pop() : null;
}
