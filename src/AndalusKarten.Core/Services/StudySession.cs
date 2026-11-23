using AndalusKarten.Core.Models;

namespace AndalusKarten.Core.Services;

/// <summary>
/// Eine Lernrunde über eine feste Kartenmenge. „Nochmal“ schiebt die Karte ein paar
/// Positionen nach hinten, sodass sie in derselben Runde wiederkommt.
/// </summary>
public sealed class StudySession
{
    public const int RequeueOffset = 4;

    private readonly LinkedList<Word> _queue;
    private readonly HashSet<string> _missedOnce = new(StringComparer.Ordinal);

    public StudySession(IEnumerable<Word> words, Random? random = null)
    {
        var rng = random ?? Random.Shared;
        var list = words.ToArray();
        rng.Shuffle(list);
        _queue = new LinkedList<Word>(list);
        Total = list.Length;
    }

    public int Total { get; }
    public int Correct { get; private set; }
    public int Wrong { get; private set; }

    /// <summary>Karten, die mindestens einmal gewusst wurden und nicht mehr in der Schlange sind.</summary>
    public int Done => Total - _queue.Count;

    public Word? Current => _queue.First?.Value;
    public bool IsFinished => _queue.Count == 0;

    /// <summary>Karten, die in dieser Runde mindestens einmal nicht gewusst wurden.</summary>
    public IReadOnlyCollection<string> MissedWordIds => _missedOnce;

    public void Answer(bool knewIt)
    {
        var node = _queue.First ?? throw new InvalidOperationException("Die Runde ist bereits beendet.");
        _queue.RemoveFirst();

        if (knewIt)
        {
            Correct++;
            return;
        }

        Wrong++;
        _missedOnce.Add(node.Value.Id);

        var target = _queue.First;
        for (var i = 1; i < RequeueOffset && target is not null; i++)
        {
            target = target.Next;
        }

        if (target is null)
        {
            _queue.AddLast(node);
        }
        else
        {
            _queue.AddAfter(target, node);
        }
    }
}
