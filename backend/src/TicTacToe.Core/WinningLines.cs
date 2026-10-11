namespace TicTacToe.Core;

/// <summary>The 8 winning lines as cell indices: 3 rows, 3 columns, 2 diagonals.</summary>
internal static class WinningLines
{
    public static readonly IReadOnlyList<int[]> All =
    [
        [0, 1, 2], [3, 4, 5], [6, 7, 8],
        [0, 3, 6], [1, 4, 7], [2, 5, 8],
        [0, 4, 8], [2, 4, 6],
    ];
}
