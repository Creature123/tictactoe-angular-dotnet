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

        // Derived once per instance. The instance is immutable, so these can never drift from Moves.
        var cells = new Player?[9];
        foreach (var move in moves)
        {
            cells[move.Position.Index] = move.Player;
        }

        Board = Array.AsReadOnly(cells);
        WinningCells = WinningLines.All
            .Where(line => cells[line[0]] is not null && line.All(cell => cells[cell] == cells[line[0]]))
            .SelectMany(line => line)
            .Distinct()
            .Order()
            .ToArray();
    }

    public Guid Id { get; }

    public GameMode Mode { get; }

    public IReadOnlyList<Move> Moves { get; }

    /// <summary>9 cells, index = row * 3 + col; null = empty.</summary>
    public IReadOnlyList<Player?> Board { get; }

    /// <summary>X moves first: X when the move count is even, O when it is odd.</summary>
    public Player CurrentPlayer => Moves.Count % 2 == 0 ? Player.X : Player.O;

    /// <summary>A win beats a draw: a 9th move that completes a line is Won.</summary>
    public GameStatus Status =>
        WinningCells.Count > 0 ? GameStatus.Won
        : Moves.Count == 9 ? GameStatus.Draw
        : GameStatus.InProgress;

    /// <summary>Only the player who just moved can complete a line, so the winner is the last mover.</summary>
    public Player? Winner => Status == GameStatus.Won ? Moves[^1].Player : null;

    /// <summary>Union of the cells of every completed line, ascending (A-09).</summary>
    public IReadOnlyList<int> WinningCells { get; }

    /// <summary>Option A: no undo once the game is complete (ADR-00).</summary>
    public bool CanUndo => Status == GameStatus.InProgress && Moves.Count > 0;

    public static Game Create(GameMode mode) => new(Guid.NewGuid(), mode, []);

    /// <summary>Plays a move. In VsComputer mode the computer (O) replies inside the same call unless the game is over.</summary>
    public GameResult Play(Player player, Position position)
    {
        if (Validate(player, position) is { } error)
        {
            return GameResult.Failure(error);
        }

        var next = Append(player, position);
        if (Mode == GameMode.VsComputer && next.Status == GameStatus.InProgress)
        {
            next = next.Append(Player.O, Position.FromIndex(ComputerStrategy.ChooseCell(next.Board)));
        }

        return GameResult.Success(next);
    }

    /// <summary>Removes the last move (TwoPlayer) or the last X/O pair (VsComputer).</summary>
    public GameResult Undo()
    {
        if (Status != GameStatus.InProgress)
        {
            return GameResult.Failure(GameError.GameCompleted);
        }

        if (Moves.Count == 0)
        {
            return GameResult.Failure(GameError.NothingToUndo);
        }

        // An in-progress VsComputer game always has an even move count (SPEC I-4), so a pair is always there.
        var removeCount = Mode == GameMode.VsComputer ? 2 : 1;
        return GameResult.Success(new Game(Id, Mode, [.. Moves.Take(Moves.Count - removeCount)]));
    }

    /// <summary>Clears every move; keeps the id and mode (A-02, A-03).</summary>
    public Game Reset() => new(Id, Mode, []);

    /// <summary>Checks the rules in the fixed order of SPEC §4 (A-10) and returns the first violation.</summary>
    private GameError? Validate(Player player, Position position)
    {
        if (!position.IsOnBoard)
        {
            return GameError.OutOfBounds;
        }

        if (Status != GameStatus.InProgress)
        {
            return GameError.GameCompleted;
        }

        // In VsComputer the computer owns O, so a human O move is never accepted (T-12).
        if (player != CurrentPlayer || (Mode == GameMode.VsComputer && player == Player.O))
        {
            return GameError.NotYourTurn;
        }

        if (Board[position.Index] is not null)
        {
            return GameError.CellOccupied;
        }

        return null;
    }

    private Game Append(Player player, Position position) =>
        new(Id, Mode, [.. Moves, new Move(Moves.Count + 1, player, position)]);
}
