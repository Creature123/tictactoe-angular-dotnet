namespace TicTacToe.Core;

/// <summary>
/// The outcome of a <see cref="GameService"/> change: the new game or the violated rule, plus the scoreboard read
/// under the same game lock, so a response never pairs a win with a scoreboard that does not yet count it.
/// </summary>
public readonly record struct GameUpdate(Game? Game, GameError? Error, ScoreSnapshot Scoreboard);
