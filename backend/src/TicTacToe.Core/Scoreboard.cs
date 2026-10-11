namespace TicTacToe.Core;

/// <summary>Session scoreboard, one per API process (A-01).</summary>
public sealed class Scoreboard
{
    /// <summary>Counts a completed game. Called once, on the InProgress → Won/Draw transition.</summary>
    public void Record(Game completedGame) => throw new NotImplementedException();

    public void Reset() => throw new NotImplementedException();

    public ScoreSnapshot Snapshot() => throw new NotImplementedException();
}
