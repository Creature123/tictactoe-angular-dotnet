using TicTacToe.Core;
using static TicTacToe.Core.Tests.GameBuilder;

namespace TicTacToe.Core.Tests;

public sealed class GameServiceTests
{
    private readonly GameService _service = new(new InMemoryGameRepository(), new Scoreboard());

    // ---- Games ---------------------------------------------------------------------------------

    [Theory]
    [Trait("Req", "MH-11")]
    [InlineData(GameMode.TwoPlayer)]
    [InlineData(GameMode.VsComputer)]
    public void CreateGame_WithMode_ReturnsGameInThatMode(GameMode mode)
    {
        var game = _service.CreateGame(mode);

        Assert.Equal(mode, game.Mode);
        Assert.Empty(game.Moves);
    }

    [Fact]
    [Trait("Req", "BE-02")]
    public void GetGame_AfterMove_ReturnsStoredMove()
    {
        var id = _service.CreateGame(GameMode.TwoPlayer).Id;
        _service.MakeMove(id, Player.X, new Position(0, 0)).Ok();

        var stored = _service.GetGame(id);

        Assert.NotNull(stored);
        Assert.Equal([new Move(1, Player.X, new Position(0, 0))], stored.Moves);
    }

    [Fact]
    [Trait("Req", "BE-09")]
    public void MakeMove_UnknownId_ReturnsGameNotFound()
    {
        var result = _service.MakeMove(Guid.NewGuid(), Player.X, new Position(0, 0));

        Assert.Equal(GameError.GameNotFound, result.Error);
    }

    [Fact]
    [Trait("Req", "BE-09")]
    public void Undo_UnknownId_ReturnsGameNotFound()
    {
        var result = _service.Undo(Guid.NewGuid());

        Assert.Equal(GameError.GameNotFound, result.Error);
    }

    [Fact]
    [Trait("Req", "BE-09")]
    public void Reset_UnknownId_ReturnsGameNotFound()
    {
        var result = _service.Reset(Guid.NewGuid());

        Assert.Equal(GameError.GameNotFound, result.Error);
    }

    // ---- Scoreboard ----------------------------------------------------------------------------

    [Fact]
    [Trait("Req", "MH-08")]
    public void GetScoreboard_NewService_AllZero()
    {
        Assert.Equal(new ScoreSnapshot(0, 0, 0), _service.GetScoreboard());
    }

    [Fact]
    [Trait("Req", "FR-14")]
    public void MakeMove_XWins_IncrementsXWinsOnce()
    {
        var id = _service.CreateGame(GameMode.TwoPlayer).Id;

        // X wins the top row on move 5.
        PlayTwoPlayer(id, (0, 0), (1, 0), (0, 1), (1, 1), (0, 2));

        Assert.Equal(new ScoreSnapshot(1, 0, 0), _service.GetScoreboard());
    }

    [Fact]
    [Trait("Req", "FR-14")]
    public void MakeMove_OWins_IncrementsOWinsOnce()
    {
        var id = _service.CreateGame(GameMode.TwoPlayer).Id;

        // O wins the middle row on move 6.
        PlayTwoPlayer(id, (0, 0), (1, 0), (0, 1), (1, 1), (2, 2), (1, 2));

        Assert.Equal(new ScoreSnapshot(0, 1, 0), _service.GetScoreboard());
    }

    [Fact]
    [Trait("Req", "FR-14")]
    [Trait("Req", "T-06")]
    public void MakeMove_ComputerWins_IncrementsOWinsOnce()
    {
        var id = _service.CreateGame(GameMode.VsComputer).Id;

        // X0 → O4 (center), X1 → O2 (block), X3 → O6 (win on 2-4-6 beats blocking X's 0-3-6).
        var game = PlayVsComputer(id, (0, 0), (0, 1), (1, 0));

        Assert.Equal(Player.O, game.Winner);
        Assert.Equal(new ScoreSnapshot(0, 1, 0), _service.GetScoreboard());
    }

    [Fact]
    [Trait("Req", "FR-14")]
    [Trait("Req", "T-07")]
    public void MakeMove_XBeatsComputer_IncrementsXWinsOnce()
    {
        var id = _service.CreateGame(GameMode.VsComputer).Id;

        // X0 → O4, X8 → O2, X6 → O3 (blocks 3 of the 3/7 fork), X7 wins the bottom row.
        var game = PlayVsComputer(id, (0, 0), (2, 2), (2, 0), (2, 1));

        Assert.Equal(Player.X, game.Winner);
        Assert.Equal(new ScoreSnapshot(1, 0, 0), _service.GetScoreboard());
    }

    [Fact]
    [Trait("Req", "FR-17")]
    public void MakeMove_Draw_IncrementsDrawsOnce()
    {
        var id = _service.CreateGame(GameMode.TwoPlayer).Id;

        PlayTwoPlayer(id, (0, 0), (0, 1), (0, 2), (1, 1), (1, 0), (1, 2), (2, 1), (2, 0), (2, 2));

        Assert.Equal(new ScoreSnapshot(0, 0, 1), _service.GetScoreboard());
    }

    [Fact]
    [Trait("Req", "MH-09")]
    public void MakeMove_AfterWin_RejectedAndScoreboardUnchanged()
    {
        var id = _service.CreateGame(GameMode.TwoPlayer).Id;
        PlayTwoPlayer(id, (0, 0), (1, 0), (0, 1), (1, 1), (0, 2));

        var result = _service.MakeMove(id, Player.O, new Position(2, 2));

        Assert.Equal(GameError.GameCompleted, result.Error);
        Assert.Equal(new ScoreSnapshot(1, 0, 0), _service.GetScoreboard());
        Assert.Equal(5, _service.GetGame(id)?.Moves.Count);
    }

    [Fact]
    [Trait("Req", "MH-09")]
    [Trait("Req", "MH-07")]
    public void Undo_AfterWin_RejectedAndScoreboardUnchanged()
    {
        var id = _service.CreateGame(GameMode.TwoPlayer).Id;
        PlayTwoPlayer(id, (0, 0), (1, 0), (0, 1), (1, 1), (0, 2));

        var result = _service.Undo(id);

        Assert.Equal(GameError.GameCompleted, result.Error);
        Assert.Equal(new ScoreSnapshot(1, 0, 0), _service.GetScoreboard());
        Assert.Equal(5, _service.GetGame(id)?.Moves.Count);
    }

    [Fact]
    [Trait("Req", "MH-10")]
    public void ResetScoreboard_NonZeroCounts_AllZeroAndGameUnchanged()
    {
        var id = _service.CreateGame(GameMode.TwoPlayer).Id;
        PlayTwoPlayer(id, (0, 0), (1, 0), (0, 1), (1, 1), (0, 2));

        var reset = _service.ResetScoreboard();

        Assert.Equal(new ScoreSnapshot(0, 0, 0), reset);
        Assert.Equal(new ScoreSnapshot(0, 0, 0), _service.GetScoreboard());
        var game = _service.GetGame(id);
        Assert.NotNull(game);
        Assert.Equal(GameStatus.Won, game.Status);
        Assert.Equal(5, game.Moves.Count);
    }

    [Fact]
    [Trait("Req", "FR-18")]
    public void Reset_AfterWin_ScoreboardUnchanged()
    {
        var id = _service.CreateGame(GameMode.TwoPlayer).Id;
        PlayTwoPlayer(id, (0, 0), (1, 0), (0, 1), (1, 1), (0, 2));

        var game = _service.Reset(id).Ok();

        Assert.Empty(game.Moves);
        Assert.Equal(GameStatus.InProgress, game.Status);
        Assert.Equal(new ScoreSnapshot(1, 0, 0), _service.GetScoreboard());
    }

    // ---- Helpers -------------------------------------------------------------------------------

    /// <summary>Plays the moves through the service, alternating X and O, and returns the final game.</summary>
    private Game PlayTwoPlayer(Guid id, params (int Row, int Col)[] moves)
    {
        Game? game = null;
        for (var i = 0; i < moves.Length; i++)
        {
            var player = i % 2 == 0 ? Player.X : Player.O;
            game = _service.MakeMove(id, player, new Position(moves[i].Row, moves[i].Col)).Ok();
        }

        Assert.NotNull(game);
        return game;
    }

    /// <summary>Plays human (X) moves through the service; the computer replies to each. Returns the final game.</summary>
    private Game PlayVsComputer(Guid id, params (int Row, int Col)[] humanMoves)
    {
        Game? game = null;
        foreach (var (row, col) in humanMoves)
        {
            game = _service.MakeMove(id, Player.X, new Position(row, col)).Ok();
        }

        Assert.NotNull(game);
        return game;
    }
}
