namespace AndalusKarten.Core.Tests;

public class StudySessionTests
{
    [Fact]
    public void Knowing_every_card_finishes_after_total_answers()
    {
        var session = new StudySession(Words.Many(5), new Random(1));

        for (var i = 0; i < 5; i++)
        {
            Assert.NotNull(session.Current);
            session.Answer(true);
        }

        Assert.True(session.IsFinished);
        Assert.Equal(5, session.Correct);
        Assert.Equal(5, session.Done);
        Assert.Null(session.Current);
    }

    [Fact]
    public void Missed_card_comes_back_after_offset()
    {
        var session = new StudySession(Words.Many(10), new Random(1));
        var missed = session.Current!;

        session.Answer(false);

        for (var i = 0; i < StudySession.RequeueOffset; i++)
        {
            Assert.NotEqual(missed.Id, session.Current!.Id);
            session.Answer(true);
        }

        Assert.Equal(missed.Id, session.Current!.Id);
        Assert.Contains(missed.Id, session.MissedWordIds);
    }

    [Fact]
    public void Missed_card_in_short_queue_goes_to_end()
    {
        var session = new StudySession(Words.Many(2), new Random(1));
        var first = session.Current!;

        session.Answer(false);
        session.Answer(true);

        Assert.Equal(first.Id, session.Current!.Id);
        session.Answer(true);
        Assert.True(session.IsFinished);
        Assert.Equal(1, session.Wrong);
    }

    [Fact]
    public void Answering_finished_session_throws()
    {
        var session = new StudySession([], new Random(1));
        Assert.True(session.IsFinished);
        Assert.Throws<InvalidOperationException>(() => session.Answer(true));
    }
}
