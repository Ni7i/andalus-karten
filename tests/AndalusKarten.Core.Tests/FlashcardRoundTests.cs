namespace AndalusKarten.Core.Tests;

public class FlashcardRoundTests
{
    [Fact]
    public void Counts_swipes_and_collects_still_learning_in_order()
    {
        var round = new FlashcardRound(Words.Many(4), shuffle: false);

        round.Answer(true);
        round.Answer(false);
        round.Answer(true);
        round.Answer(false);

        Assert.True(round.IsFinished);
        Assert.Equal(2, round.KnownCount);
        Assert.Equal(2, round.LearningCount);
        Assert.Equal(["w2", "w4"], round.StillLearning.Select(w => w.Id));
    }

    [Fact]
    public void Undo_restores_previous_card()
    {
        var round = new FlashcardRound(Words.Many(3), shuffle: false);
        round.Answer(false);

        var undone = round.Undo();

        Assert.Equal(("w1", false), (undone!.Value.Word.Id, undone.Value.Knew));
        Assert.Equal("w1", round.Current!.Id);
        Assert.Equal(0, round.LearningCount);
        Assert.False(round.CanUndo);
        Assert.Null(round.Undo());
    }

    [Fact]
    public void Shuffle_keeps_all_cards()
    {
        var round = new FlashcardRound(Words.Many(20), shuffle: true, new Random(5));
        var seen = new List<string>();
        while (!round.IsFinished) { seen.Add(round.Current!.Id); round.Answer(true); }
        Assert.Equal(Words.Many(20).Select(w => w.Id).Order(), seen.Order());
    }
}

public class QuizBuilderTests
{
    private static Word W(string id, string de, string ar) => new() { Id = id, German = de, Arabic = ar };

    [Fact]
    public void Choice_contains_answer_once_and_no_duplicate_meanings()
    {
        var pool = new[] { W("1", "Book", "كتاب"), W("2", "Pen", "قلم"), W("3", "pen", "قلم2"), W("4", "House", "بيت"), W("5", "Car", "سيارة"), W("6", "Book", "سفر") };

        for (var seed = 0; seed < 20; seed++)
        {
            var q = QuizBuilder.Choice(pool[0], pool, new Random(seed));
            Assert.Equal(4, q.Options.Count);
            Assert.Equal("1", q.Options[q.CorrectIndex].Id);
            Assert.Equal(4, q.Options.Select(o => o.German.ToLowerInvariant()).Distinct().Count());
        }
    }

    [Fact]
    public void Choice_with_small_pool_has_fewer_options()
    {
        var pool = new[] { W("1", "a", "x"), W("2", "b", "y") };
        var q = QuizBuilder.Choice(pool[0], pool, new Random(1));
        Assert.Equal(2, q.Options.Count);
    }

    [Fact]
    public void MatchSet_has_unique_meanings()
    {
        var pool = Enumerable.Range(1, 30).Select(i => W($"{i}", $"m{i % 10}", $"a{i}")).ToList();
        var set = QuizBuilder.MatchSet(pool, new Random(2));
        Assert.Equal(6, set.Count);
        Assert.Equal(6, set.Select(w => w.German).Distinct().Count());
    }
}
