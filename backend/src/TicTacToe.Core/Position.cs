namespace TicTacToe.Core;

/// <summary>A 0-based board position (SPEC §3).</summary>
public readonly record struct Position(int Row, int Col)
{
    /// <summary>Cell index: Row * 3 + Col.</summary>
    public int Index => throw new NotImplementedException();
}
