namespace TicTacToe.Core;

/// <summary>
/// An immutable game. The ordered move list is the only stored state; everything else is derived from it.
/// Play, Undo and Reset return a new instance.
/// </summary>
public sealed class Game
{
    private Game(Guid id, GameMode mode, IReadOnlyList<Move> moves)
    {
        Id = id;
        Mode = mode;
        Moves = moves;
    }

    public Guid Id { get; }

    public GameMode Mode { get; }

    public IReadOnlyList<Move> Moves { get; }

    /// <summary>9 cells, index = row * 3 + col; null = empty.</summary>
    public IReadOnlyList<Player?> Board => throw new NotImplementedException();

    public Player CurrentPlayer => throw new NotImplementedException();

    public GameStatus Status => throw new NotImplementedException();

    public Player? Winner => throw new NotImplementedException();

    /// <summary>Union of the cells of every completed line, ascending (A-09).</summary>
    public IReadOnlyList<int> WinningCells => throw new NotImplementedException();

    public bool CanUndo => throw new NotImplementedException();

    public static Game Create(GameMode mode) => throw new NotImplementedException();

    /// <summary>Plays a move. In VsComputer mode the computer (O) replies inside the same call unless the game is over.</summary>
    public GameResult Play(Player player, Position position) => throw new NotImplementedException();

    /// <summary>Removes the last move (TwoPlayer) or the last X/O pair (VsComputer).</summary>
    public GameResult Undo() => throw new NotImplementedException();

    /// <summary>Clears every move; keeps the id and mode (A-02, A-03).</summary>
    public Game Reset() => throw new NotImplementedException();
}
