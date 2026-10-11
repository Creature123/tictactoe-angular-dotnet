using TicTacToe.Core;

namespace TicTacToe.Api.Contracts;

/// <summary>The <c>GameState</c> shape of SPEC §5.1.</summary>
public sealed record GameStateResponse(
    Guid Id,
    GameMode Mode,
    GameStatus Status,
    Player? CurrentPlayer,
    Player? Winner,
    IReadOnlyList<int> WinningCells,
    IReadOnlyList<Player?> Board,
    IReadOnlyList<MoveResponse> Moves,
    bool CanUndo,
    ScoreboardResponse Scoreboard)
{
    public static GameStateResponse From(Game game, ScoreSnapshot score) => new(
        game.Id,
        game.Mode,
        game.Status,
        // A finished game has no turn on the wire (A-08).
        game.Status == GameStatus.InProgress ? game.CurrentPlayer : null,
        game.Winner,
        game.WinningCells,
        game.Board,
        [.. game.Moves.Select(MoveResponse.From)],
        game.CanUndo,
        ScoreboardResponse.From(score));
}

/// <summary>One history entry. <c>row</c>/<c>col</c> are 0-based; <c>number</c> is 1-based.</summary>
public sealed record MoveResponse(int Number, Player Player, int Row, int Col)
{
    public static MoveResponse From(Move move) => new(move.Number, move.Player, move.Position.Row, move.Position.Col);
}
