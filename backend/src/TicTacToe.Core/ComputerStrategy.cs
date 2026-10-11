namespace TicTacToe.Core;

/// <summary>Deterministic computer move: win > block > center > corner (0, 2, 6, 8) > first free cell (SPEC §6).</summary>
public static class ComputerStrategy
{
    public static int ChooseCell(IReadOnlyList<Player?> board) => throw new NotImplementedException();
}
