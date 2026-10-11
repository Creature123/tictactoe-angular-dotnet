namespace TicTacToe.Core;

/// <summary>Session scoreboard, one per API process (A-01). Thread-safe: every access takes its own lock.</summary>
public sealed class Scoreboard
{
    private readonly Lock _lock = new();
    private ScoreSnapshot _score;

    /// <summary>
    /// Counts a completed game and returns the counts that include it. Called once, on the InProgress → Won/Draw
    /// transition.
    /// </summary>
    public ScoreSnapshot Record(Game completedGame)
    {
        lock (_lock)
        {
            _score = completedGame switch
            {
                { Status: GameStatus.Won, Winner: Player.X } => _score with { XWins = _score.XWins + 1 },
                { Status: GameStatus.Won, Winner: Player.O } => _score with { OWins = _score.OWins + 1 },
                { Status: GameStatus.Draw } => _score with { Draws = _score.Draws + 1 },
                _ => throw new ArgumentException("Only a completed game can be recorded.", nameof(completedGame)),
            };
            return _score;
        }
    }

    public void Reset()
    {
        lock (_lock)
        {
            _score = default;
        }
    }

    public ScoreSnapshot Snapshot()
    {
        lock (_lock)
        {
            return _score;
        }
    }
}
