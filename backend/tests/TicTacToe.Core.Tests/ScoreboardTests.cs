using TicTacToe.Core;
using static TicTacToe.Core.Tests.GameBuilder;

namespace TicTacToe.Core.Tests;

public sealed class ScoreboardTests
{
    private readonly Scoreboard _scoreboard = new();

    [Fact]
    [Trait("Req", "FR-14")]
    [Trait("Req", "FR-17")]
    public void Record_XWinOWinAndDraw_IncrementsEachMatchingCountOnce()
    {
        // X wins the top row; O wins the middle row; a full board with no line.
        _scoreboard.Record(TwoPlayer((0, 0), (1, 0), (0, 1), (1, 1), (0, 2)));
        _scoreboard.Record(TwoPlayer((0, 0), (1, 0), (0, 1), (1, 1), (2, 2), (1, 2)));
        _scoreboard.Record(TwoPlayer((0, 0), (0, 1), (0, 2), (1, 1), (1, 0), (1, 2), (2, 1), (2, 0), (2, 2)));

        Assert.Equal(new ScoreSnapshot(1, 1, 1), _scoreboard.Snapshot());
    }

    [Fact]
    [Trait("Req", "I-6")]
    public void Record_InProgressGame_ThrowsAndCountsUnchanged()
    {
        var inProgress = TwoPlayer((0, 0));

        Assert.Throws<ArgumentException>(() => _scoreboard.Record(inProgress));
        Assert.Equal(new ScoreSnapshot(0, 0, 0), _scoreboard.Snapshot());
    }

    [Fact]
    [Trait("Req", "MH-10")]
    public void Reset_AfterRecords_AllZero()
    {
        _scoreboard.Record(TwoPlayer((0, 0), (1, 0), (0, 1), (1, 1), (0, 2)));

        _scoreboard.Reset();

        Assert.Equal(new ScoreSnapshot(0, 0, 0), _scoreboard.Snapshot());
    }
}
