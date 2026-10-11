namespace TicTacToe.Core;

/// <summary>A 0-based board position (SPEC §3).</summary>
public readonly record struct Position(int Row, int Col)
{
    /// <summary>Cell index: Row * 3 + Col.</summary>
    public int Index => Row * 3 + Col;

    public bool IsOnBoard => Row is >= 0 and <= 2 && Col is >= 0 and <= 2;

    public static Position FromIndex(int index) => new(index / 3, index % 3);
}
