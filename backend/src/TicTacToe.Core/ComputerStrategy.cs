namespace TicTacToe.Core;

/// <summary>Deterministic computer move: win > block > center > corner (0, 2, 6, 8) > first free cell (SPEC §6).</summary>
public static class ComputerStrategy
{
    private static readonly int[] Center = [4];
    private static readonly int[] Corners = [0, 2, 6, 8];
    private static readonly int[] AllCells = [0, 1, 2, 3, 4, 5, 6, 7, 8];

    /// <summary>Picks O's cell. Call only on an in-progress board, which always has a free cell.</summary>
    public static int ChooseCell(IReadOnlyList<Player?> board) =>
        FirstEmpty(board, AllCells.Where(cell => WouldCompleteLine(board, cell, Player.O)))
        ?? FirstEmpty(board, AllCells.Where(cell => WouldCompleteLine(board, cell, Player.X)))
        ?? FirstEmpty(board, Center)
        ?? FirstEmpty(board, Corners)
        ?? FirstEmpty(board, AllCells)
        ?? throw new InvalidOperationException("The computer cannot move on a full board.");

    /// <summary>True if <paramref name="cell"/> is empty and playing it would give <paramref name="player"/> three in a line.</summary>
    private static bool WouldCompleteLine(IReadOnlyList<Player?> board, int cell, Player player) =>
        board[cell] is null && WinningLines.All.Any(line =>
            line.Contains(cell) && line.All(other => other == cell || board[other] == player));

    /// <summary>The first empty cell in the given order, or null if none is empty.</summary>
    private static int? FirstEmpty(IReadOnlyList<Player?> board, IEnumerable<int> cells)
    {
        foreach (var cell in cells)
        {
            if (board[cell] is null)
            {
                return cell;
            }
        }

        return null;
    }
}
