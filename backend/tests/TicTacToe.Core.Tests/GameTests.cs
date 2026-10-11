using TicTacToe.Core;
using static TicTacToe.Core.Tests.GameBuilder;

namespace TicTacToe.Core.Tests;

public sealed class GameTests
{
    // ---- New game -------------------------------------------------------------------------------

    [Fact]
    [Trait("Req", "FR-01")]
    public void Create_NewGame_HasNineEmptyCellsAndNoMoves()
    {
        var game = Game.Create(GameMode.TwoPlayer);

        Assert.Equal(".........", game.Board.AsText());
        Assert.Empty(game.Moves);
    }

    [Fact]
    [Trait("Req", "FR-05")]
    public void Create_NewGame_IsInProgressWithXToMove()
    {
        var game = Game.Create(GameMode.TwoPlayer);

        Assert.Equal(GameStatus.InProgress, game.Status);
        Assert.Equal(Player.X, game.CurrentPlayer);
        Assert.Null(game.Winner);
        Assert.Empty(game.WinningCells);
    }

    // ---- Valid moves and turn switching ---------------------------------------------------------

    [Fact]
    [Trait("Req", "FR-02")]
    [Trait("Req", "MH-02")]
    public void Play_ValidMoveOnEmptyCell_PlacesMarkAndAppendsMove()
    {
        var game = Game.Create(GameMode.TwoPlayer);

        var after = game.Play(Player.X, new Position(0, 0)).Ok();

        Assert.Equal("X........", after.Board.AsText());
        Assert.Equal([new Move(1, Player.X, new Position(0, 0))], after.Moves);
        Assert.Equal(GameStatus.InProgress, after.Status);
    }

    [Fact]
    [Trait("Req", "FR-07")]
    public void Play_ValidNonEndingMoveByX_SwitchesTurnToO()
    {
        var game = Game.Create(GameMode.TwoPlayer);

        var after = game.Play(Player.X, new Position(1, 1)).Ok();

        Assert.Equal(Player.O, after.CurrentPlayer);
    }

    [Fact]
    [Trait("Req", "FR-07")]
    public void Play_ValidMoveByO_SwitchesTurnBackToX()
    {
        var game = TwoPlayer((1, 1));

        var after = game.Play(Player.O, new Position(0, 0)).Ok();

        Assert.Equal(Player.X, after.CurrentPlayer);
        Assert.Equal(2, after.Moves.Count);
    }

    [Fact]
    [Trait("Req", "MH-12")]
    public void Play_TwoPlayerMode_AddsExactlyOneMoveWithNoAutoReply()
    {
        var game = Game.Create(GameMode.TwoPlayer);

        var after = game.Play(Player.X, new Position(0, 0)).Ok();

        Assert.Single(after.Moves);
        Assert.Equal(Player.O, after.CurrentPlayer);
    }

    // ---- Invalid moves: error returned, turn and history unchanged ------------------------------

    [Theory]
    [Trait("Req", "BE-05")]
    [Trait("Req", "FR-08")]
    [InlineData(-1, 0)]
    [InlineData(3, 0)]
    [InlineData(0, -1)]
    [InlineData(0, 3)]
    public void Play_OutOfBounds_ReturnsOutOfBoundsAndTurnUnchanged(int row, int col)
    {
        var game = TwoPlayer((1, 1));

        var result = game.Play(Player.O, new Position(row, col));

        Assert.Equal(GameError.OutOfBounds, result.Error);
        Assert.Null(result.Game);
        Assert.Equal(Player.O, game.CurrentPlayer);
        Assert.Single(game.Moves);
    }

    [Fact]
    [Trait("Req", "FR-04")]
    [Trait("Req", "BE-06")]
    [Trait("Req", "FR-08")]
    public void Play_OccupiedCell_ReturnsCellOccupiedAndTurnUnchanged()
    {
        var game = TwoPlayer((0, 0));

        var result = game.Play(Player.O, new Position(0, 0));

        Assert.Equal(GameError.CellOccupied, result.Error);
        Assert.Null(result.Game);
        Assert.Equal(Player.O, game.CurrentPlayer);
        Assert.Equal("X........", game.Board.AsText());
        Assert.Single(game.Moves);
    }

    [Fact]
    [Trait("Req", "BE-08")]
    [Trait("Req", "FR-08")]
    public void Play_WrongPlayer_ReturnsNotYourTurnAndTurnUnchanged()
    {
        var game = Game.Create(GameMode.TwoPlayer);

        var result = game.Play(Player.O, new Position(0, 0));

        Assert.Equal(GameError.NotYourTurn, result.Error);
        Assert.Null(result.Game);
        Assert.Equal(Player.X, game.CurrentPlayer);
        Assert.Empty(game.Moves);
    }

    [Fact]
    [Trait("Req", "BE-08")]
    public void Play_OInComputerMode_ReturnsNotYourTurn()
    {
        // After X(0,0) the computer replied, so it is X's turn again; a human can never play O (T-12).
        var game = VsComputer((0, 0));

        var result = game.Play(Player.O, new Position(2, 2));

        Assert.Equal(GameError.NotYourTurn, result.Error);
        Assert.Equal(2, game.Moves.Count);
    }

    [Fact]
    [Trait("Req", "FR-13")]
    [Trait("Req", "BE-07")]
    [Trait("Req", "FR-08")]
    public void Play_AfterWin_ReturnsGameCompletedAndStateUnchanged()
    {
        // X wins the top row.
        var game = TwoPlayer((0, 0), (1, 0), (0, 1), (1, 1), (0, 2));

        var result = game.Play(Player.O, new Position(2, 2));

        Assert.Equal(GameError.GameCompleted, result.Error);
        Assert.Null(result.Game);
        Assert.Equal(GameStatus.Won, game.Status);
        Assert.Equal(5, game.Moves.Count);
    }

    [Fact]
    [Trait("Req", "FR-13")]
    [Trait("Req", "BE-07")]
    public void Play_AfterDraw_ReturnsGameCompleted()
    {
        var game = TwoPlayer((0, 0), (0, 1), (0, 2), (1, 1), (1, 0), (1, 2), (2, 1), (2, 0), (2, 2));

        var result = game.Play(Player.O, new Position(0, 0));

        Assert.Equal(GameError.GameCompleted, result.Error);
        Assert.Equal(GameStatus.Draw, game.Status);
        Assert.Equal(9, game.Moves.Count);
    }

    [Fact]
    [Trait("Req", "A-10")]
    public void Play_OnCompletedGameAndOccupiedCell_ReturnsGameCompleted()
    {
        // X wins the top row; (0,0) is both occupied and on a finished board. GAME_COMPLETED is checked first.
        var game = TwoPlayer((0, 0), (1, 0), (0, 1), (1, 1), (0, 2));

        var result = game.Play(Player.O, new Position(0, 0));

        Assert.Equal(GameError.GameCompleted, result.Error);
    }

    // ---- Wins ----------------------------------------------------------------------------------

    [Fact]
    [Trait("Req", "FR-09")]
    public void Play_XCompletesTopRow_XWinsWithCells012()
    {
        var game = TwoPlayer((0, 0), (1, 0), (0, 1), (1, 1));

        var after = game.Play(Player.X, new Position(0, 2)).Ok();

        Assert.Equal(GameStatus.Won, after.Status);
        Assert.Equal(Player.X, after.Winner);
        Assert.Equal([0, 1, 2], after.WinningCells);
    }

    [Fact]
    [Trait("Req", "FR-09")]
    public void Play_OCompletesMiddleRow_OWinsWithCells345()
    {
        var game = TwoPlayer((0, 0), (1, 0), (0, 1), (1, 1), (2, 2));

        var after = game.Play(Player.O, new Position(1, 2)).Ok();

        Assert.Equal(GameStatus.Won, after.Status);
        Assert.Equal(Player.O, after.Winner);
        Assert.Equal([3, 4, 5], after.WinningCells);
    }

    [Fact]
    [Trait("Req", "FR-10")]
    public void Play_XCompletesLeftColumn_XWinsWithCells036()
    {
        var game = TwoPlayer((0, 0), (0, 1), (1, 0), (1, 1));

        var after = game.Play(Player.X, new Position(2, 0)).Ok();

        Assert.Equal(GameStatus.Won, after.Status);
        Assert.Equal(Player.X, after.Winner);
        Assert.Equal([0, 3, 6], after.WinningCells);
    }

    [Fact]
    [Trait("Req", "FR-11")]
    public void Play_XCompletesMainDiagonal_WinningCells048()
    {
        var game = TwoPlayer((0, 0), (0, 1), (1, 1), (0, 2));

        var after = game.Play(Player.X, new Position(2, 2)).Ok();

        Assert.Equal(GameStatus.Won, after.Status);
        Assert.Equal(Player.X, after.Winner);
        Assert.Equal([0, 4, 8], after.WinningCells);
    }

    [Fact]
    [Trait("Req", "FR-11")]
    public void Play_XCompletesAntiDiagonal_WinningCells246()
    {
        var game = TwoPlayer((0, 2), (0, 0), (1, 1), (0, 1));

        var after = game.Play(Player.X, new Position(2, 0)).Ok();

        Assert.Equal(GameStatus.Won, after.Status);
        Assert.Equal(Player.X, after.Winner);
        Assert.Equal([2, 4, 6], after.WinningCells);
    }

    [Fact]
    [Trait("Req", "A-09")]
    public void Play_MoveCompletesTwoLines_WinningCellsIsUnion()
    {
        // X holds all four corners; the center completes both diagonals at once.
        var game = TwoPlayer((0, 0), (0, 1), (0, 2), (1, 0), (2, 0), (2, 1), (2, 2), (1, 2));

        var after = game.Play(Player.X, new Position(1, 1)).Ok();

        Assert.Equal(GameStatus.Won, after.Status);
        Assert.Equal([0, 2, 4, 6, 8], after.WinningCells);
    }

    // ---- Draw ----------------------------------------------------------------------------------

    [Fact]
    [Trait("Req", "FR-15")]
    public void Play_NinthMoveWithNoLine_IsDrawWithNoWinner()
    {
        var game = TwoPlayer((0, 0), (0, 1), (0, 2), (1, 1), (1, 0), (1, 2), (2, 1), (2, 0));

        var after = game.Play(Player.X, new Position(2, 2)).Ok();

        Assert.Equal("XOXXOOOXX", after.Board.AsText());
        Assert.Equal(GameStatus.Draw, after.Status);
        Assert.Null(after.Winner);
        Assert.Empty(after.WinningCells);
    }

    [Fact]
    [Trait("Req", "FR-15")]
    public void Play_NinthMoveCompletesLine_IsWonNotDraw()
    {
        var game = TwoPlayer((0, 0), (0, 1), (0, 2), (1, 1), (2, 1), (1, 0), (1, 2), (2, 0));

        var after = game.Play(Player.X, new Position(2, 2)).Ok();

        Assert.Equal(9, after.Moves.Count);
        Assert.Equal(GameStatus.Won, after.Status);
        Assert.Equal(Player.X, after.Winner);
        Assert.Equal([2, 5, 8], after.WinningCells);
    }

    // ---- Reset ---------------------------------------------------------------------------------

    [Fact]
    [Trait("Req", "FR-18")]
    [Trait("Req", "FR-05")]
    public void Reset_InProgressGame_ClearsMovesAndBoardWithXToMove()
    {
        var game = TwoPlayer((0, 0), (1, 1), (2, 2));

        var after = game.Reset();

        Assert.Empty(after.Moves);
        Assert.Equal(".........", after.Board.AsText());
        Assert.Equal(GameStatus.InProgress, after.Status);
        Assert.Equal(Player.X, after.CurrentPlayer);
    }

    [Fact]
    [Trait("Req", "FR-18")]
    public void Reset_CompletedGame_IsInProgressWithNoWinner()
    {
        var game = TwoPlayer((0, 0), (1, 0), (0, 1), (1, 1), (0, 2));

        var after = game.Reset();

        Assert.Equal(GameStatus.InProgress, after.Status);
        Assert.Null(after.Winner);
        Assert.Empty(after.WinningCells);
        Assert.Empty(after.Moves);
        Assert.Equal(Player.X, after.CurrentPlayer);
    }

    [Fact]
    [Trait("Req", "FR-18")]
    public void Reset_ComputerGame_KeepsIdAndMode()
    {
        var game = VsComputer((0, 0));

        var after = game.Reset();

        Assert.Equal(game.Id, after.Id);
        Assert.Equal(GameMode.VsComputer, after.Mode);
        Assert.Empty(after.Moves);
    }

    // ---- Undo: Two Player ----------------------------------------------------------------------

    [Fact]
    [Trait("Req", "MH-03")]
    public void Undo_TwoPlayerAfterTwoMoves_RemovesLastMoveAndOToMove()
    {
        var game = TwoPlayer((0, 0), (1, 1));

        var after = game.Undo().Ok();

        Assert.Equal("X........", after.Board.AsText());
        Assert.Equal(Player.O, after.CurrentPlayer);
        Assert.Equal([new Move(1, Player.X, new Position(0, 0))], after.Moves);
        Assert.Equal(GameStatus.InProgress, after.Status);
    }

    [Fact]
    [Trait("Req", "MH-05")]
    public void Undo_TwoPlayer_EqualsFreshGameReplayedWithRemainingMoves()
    {
        var game = TwoPlayer((0, 0), (1, 1), (2, 2));
        var expected = TwoPlayer((0, 0), (1, 1));

        var after = game.Undo().Ok();

        Assert.Equal(expected.Moves, after.Moves);
        Assert.Equal(expected.Board, after.Board);
        Assert.Equal(expected.CurrentPlayer, after.CurrentPlayer);
        Assert.Equal(expected.Status, after.Status);
        Assert.Equal(expected.CanUndo, after.CanUndo);
    }

    [Fact]
    [Trait("Req", "MH-06")]
    public void Undo_NoMoves_ReturnsNothingToUndo()
    {
        var game = Game.Create(GameMode.TwoPlayer);

        var result = game.Undo();

        Assert.Equal(GameError.NothingToUndo, result.Error);
        Assert.Null(result.Game);
    }

    [Fact]
    [Trait("Req", "MH-06")]
    public void CanUndo_NewGame_IsFalse()
    {
        var game = Game.Create(GameMode.TwoPlayer);

        Assert.False(game.CanUndo);
    }

    [Fact]
    [Trait("Req", "MH-07")]
    public void Undo_AfterWin_ReturnsGameCompletedAndStateUnchanged()
    {
        var game = TwoPlayer((0, 0), (1, 0), (0, 1), (1, 1), (0, 2));

        var result = game.Undo();

        Assert.Equal(GameError.GameCompleted, result.Error);
        Assert.Null(result.Game);
        Assert.Equal(GameStatus.Won, game.Status);
        Assert.Equal(5, game.Moves.Count);
    }

    [Fact]
    [Trait("Req", "MH-07")]
    public void CanUndo_CompletedGame_IsFalse()
    {
        var game = TwoPlayer((0, 0), (1, 0), (0, 1), (1, 1), (0, 2));

        Assert.False(game.CanUndo);
    }

    // ---- Computer mode: reply and undo ---------------------------------------------------------

    [Fact]
    [Trait("Req", "MH-13")]
    public void Play_ComputerModeNonEndingMove_AppendsXThenOAndXToMove()
    {
        var game = Game.Create(GameMode.VsComputer);

        var after = game.Play(Player.X, new Position(0, 0)).Ok();

        // The computer takes the center (no win or block possible yet).
        Assert.Equal(
            [new Move(1, Player.X, new Position(0, 0)), new Move(2, Player.O, new Position(1, 1))],
            after.Moves);
        Assert.Equal(Player.X, after.CurrentPlayer);
    }

    [Fact]
    [Trait("Req", "MH-15")]
    public void Play_ComputerModeXWins_ComputerDoesNotReply()
    {
        // X0 → O4 (center), X8 → O2 (corner), X6 → O3 (blocks lowest of the 3/7 fork). X7 then wins the bottom row.
        var game = VsComputer((0, 0), (2, 2), (2, 0));

        var after = game.Play(Player.X, new Position(2, 1)).Ok();

        Assert.Equal(GameStatus.Won, after.Status);
        Assert.Equal(Player.X, after.Winner);
        Assert.Equal([6, 7, 8], after.WinningCells);
        Assert.Equal(7, after.Moves.Count);
        Assert.Equal(Player.X, after.Moves[^1].Player);
    }

    [Fact]
    [Trait("Req", "MH-04")]
    public void Undo_ComputerModeAfterOneRound_RemovesBothMovesAndXToMove()
    {
        var game = VsComputer((0, 0));

        var after = game.Undo().Ok();

        Assert.Equal(".........", after.Board.AsText());
        Assert.Equal(Player.X, after.CurrentPlayer);
        Assert.Empty(after.Moves);
        Assert.Equal(GameStatus.InProgress, after.Status);
    }

    [Fact]
    [Trait("Req", "MH-04")]
    [Trait("Req", "MH-05")]
    public void Undo_ComputerModeAfterTwoRounds_BoardAsBeforeSecondXMove()
    {
        // X0 → O4 (center), X8 → O2 (corner). Undo removes O2 and X8.
        var game = VsComputer((0, 0), (2, 2));

        var after = game.Undo().Ok();

        Assert.Equal("X...O....", after.Board.AsText());
        Assert.Equal(Player.X, after.CurrentPlayer);
        Assert.Equal(2, after.Moves.Count);
        Assert.Equal(GameStatus.InProgress, after.Status);
    }
}
