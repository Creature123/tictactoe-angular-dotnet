using TicTacToe.Core;

namespace TicTacToe.Core.Tests;

/// <summary>Builds games from explicit, replayable move sequences.</summary>
internal static class GameBuilder
{
    /// <summary>A TwoPlayer game after the given moves; players alternate starting with X.</summary>
    public static Game TwoPlayer(params (int Row, int Col)[] moves) => Play(Game.Create(GameMode.TwoPlayer), moves);

    /// <summary>A VsComputer game after the given human (X) moves; the computer replies to each one.</summary>
    public static Game VsComputer(params (int Row, int Col)[] humanMoves) => Play(Game.Create(GameMode.VsComputer), humanMoves);

    /// <summary>Plays each move for the player whose turn it is, asserting every move is accepted.</summary>
    public static Game Play(Game game, params (int Row, int Col)[] moves)
    {
        foreach (var (row, col) in moves)
        {
            game = game.Play(game.CurrentPlayer, new Position(row, col)).Ok();
        }

        return game;
    }

    /// <summary>Asserts the result is a success and returns the new game.</summary>
    public static Game Ok(this GameResult result)
    {
        Assert.Null(result.Error);
        Assert.NotNull(result.Game);
        return result.Game;
    }

    /// <summary>The board as 9 characters ("X", "O" or "." per cell), e.g. "XX.OO...X".</summary>
    public static string AsText(this IReadOnlyList<Player?> board) =>
        string.Concat(board.Select(cell => cell?.ToString() ?? "."));
}
