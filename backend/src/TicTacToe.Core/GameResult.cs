namespace TicTacToe.Core;

/// <summary>Either the new game state (<see cref="Game"/>) or the rule that was violated (<see cref="Error"/>).</summary>
public readonly record struct GameResult(Game? Game, GameError? Error)
{
    public static GameResult Success(Game game) => new(game, null);

    public static GameResult Failure(GameError error) => new(null, error);
}
