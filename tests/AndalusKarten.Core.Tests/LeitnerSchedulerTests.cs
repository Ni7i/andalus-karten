namespace AndalusKarten.Core.Tests;

public class LeitnerSchedulerTests
{
    private static readonly DateOnly Today = new(2026, 11, 23);
    private readonly LeitnerScheduler _sut = new();

    [Theory]
    [InlineData(0, 1, 1)]
    [InlineData(1, 2, 2)]
    [InlineData(2, 3, 4)]
    [InlineData(3, 4, 8)]
    [InlineData(4, 5, 16)]
    [InlineData(5, 5, 16)]
    public void Correct_answer_moves_card_up_and_spaces_it(int box, int expectedBox, int expectedDays)
    {
        var result = _sut.Grade(new CardProgress(box, Today, 0, 0), knewIt: true, Today);

        Assert.Equal(expectedBox, result.Box);
        Assert.Equal(Today.AddDays(expectedDays), result.DueOn);
        Assert.Equal(1, result.TimesCorrect);
    }

    [Fact]
    public void Wrong_answer_resets_to_box_one_due_today()
    {
        var result = _sut.Grade(new CardProgress(4, Today.AddDays(5), 3, 0), knewIt: false, Today);

        Assert.Equal(1, result.Box);
        Assert.Equal(Today, result.DueOn);
        Assert.Equal(1, result.TimesWrong);
        Assert.Equal(3, result.TimesCorrect);
    }
}
