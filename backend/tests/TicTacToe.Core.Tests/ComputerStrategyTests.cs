using TicTacToe.Core;
using static TicTacToe.Core.Tests.GameBuilder;

namespace TicTacToe.Core.Tests;

/// <summary>
/// One test per §6 priority rule. Each board is built from TwoPlayer moves (X first) and leaves O to move.
/// The board text in each comment uses cell indices 0–8, with "." for an empty cell.
/// </summary>
public sealed class ComputerStrategyTests
{
    [Fact]
    [Trait("Req", "MH-16")]
    public void ChooseCell_OCanWinAndXHasNoThreat_PlaysWinningCell()
    {
        // OXXXO....: O wins on the 0-4-8 diagonal at 8; X has no open two-in-a-line, so blocking is not in play.
        var board = TwoPlayer((0, 1), (0, 0), (0, 2), (1, 1), (1, 0)).Board;

        Assert.Equal(8, ComputerStrategy.ChooseCell(board));
    }

    [Fact]
    [Trait("Req", "MH-16")]
    public void ChooseCell_OCanWinAndXCanWin_PrefersWin()
    {
        // §6-E1 XX.OO...X: O wins at 5, X threatens 2. Win beats block.
        var board = TwoPlayer((0, 0), (1, 0), (0, 1), (1, 1), (2, 2)).Board;

        Assert.Equal(5, ComputerStrategy.ChooseCell(board));
    }

    [Fact]
    [Trait("Req", "MH-17")]
    public void ChooseCell_XThreatensLine_Blocks()
    {
        // §6-E2 XX.O.....: O has no win; X threatens 2. Blocking beats the free center.
        var board = TwoPlayer((0, 0), (1, 0), (0, 1)).Board;

        Assert.Equal(2, ComputerStrategy.ChooseCell(board));
    }

    [Fact]
    [Trait("Req", "MH-18")]
    public void ChooseCell_NoWinOrBlockAndCenterFree_PlaysCenter()
    {
        // §6-E3 X........: no lines with two marks, so the center rule decides.
        var board = TwoPlayer((0, 0)).Board;

        Assert.Equal(4, ComputerStrategy.ChooseCell(board));
    }

    [Fact]
    [Trait("Req", "MH-19")]
    public void ChooseCell_CenterTaken_PlaysFirstCorner()
    {
        // §6-E4 ....X....: no win or block, center taken, so the first corner in order (0) decides.
        var board = TwoPlayer((1, 1)).Board;

        Assert.Equal(0, ComputerStrategy.ChooseCell(board));
    }

    [Fact]
    [Trait("Req", "MH-19")]
    public void ChooseCell_CornerZeroTaken_PlaysNextCornerInOrder()
    {
        // O...X...X: X's 0-4-8 line is blocked by O, center taken, corner 0 taken, so the next corner (2) decides.
        var board = TwoPlayer((1, 1), (0, 0), (2, 2)).Board;

        Assert.Equal(2, ComputerStrategy.ChooseCell(board));
    }

    [Fact]
    [Trait("Req", "MH-20")]
    public void ChooseCell_NoCornerFree_PlaysLowestFreeCell()
    {
        // §6-E5 XOX.X.OXO: free cells 3 and 5 neither win nor block; center and corners are taken, so the lowest (3) decides.
        var board = TwoPlayer((0, 0), (0, 1), (0, 2), (2, 0), (1, 1), (2, 2), (2, 1)).Board;

        Assert.Equal(3, ComputerStrategy.ChooseCell(board));
    }

    [Fact]
    [Trait("Req", "MH-21")]
    public void ChooseCell_TwoWinningCells_PlaysLowest()
    {
        // §6-T1 XOXOO.X.X: O wins at 5 (3-4-5) or 7 (1-4-7). The tie-break picks the lowest index.
        var board = TwoPlayer((0, 0), (0, 1), (0, 2), (1, 0), (2, 0), (1, 1), (2, 2)).Board;

        Assert.Equal(5, ComputerStrategy.ChooseCell(board));
    }

    [Fact]
    [Trait("Req", "MH-21")]
    public void ChooseCell_TwoBlockingCells_PlaysLowest()
    {
        // §6-T2 XOXOX....: O has no win; X threatens 6 (2-4-6) and 8 (0-4-8). The tie-break picks the lowest index.
        var board = TwoPlayer((0, 0), (0, 1), (0, 2), (1, 0), (1, 1)).Board;

        Assert.Equal(6, ComputerStrategy.ChooseCell(board));
    }

    public static TheoryData<string> ExampleBoards => new()
    {
        "0,1 0,0 0,2 1,1 1,0",
        "0,0 1,0 0,1 1,1 2,2",
        "0,0 1,0 0,1",
        "0,0",
        "1,1",
        "1,1 0,0 2,2",
        "0,0 0,1 0,2 2,0 1,1 2,2 2,1",
        "0,0 0,1 0,2 1,0 2,0 1,1 2,2",
        "0,0 0,1 0,2 1,0 1,1",
    };

    [Theory]
    [Trait("Req", "MH-14")]
    [MemberData(nameof(ExampleBoards))]
    public void ChooseCell_AnyExampleBoard_ReturnsEmptyCellInRange(string moves)
    {
        var board = TwoPlayer(ParseMoves(moves)).Board;

        var cell = ComputerStrategy.ChooseCell(board);

        Assert.InRange(cell, 0, 8);
        Assert.Null(board[cell]);
    }

    private static (int Row, int Col)[] ParseMoves(string moves) =>
        moves.Split(' ')
            .Select(move => move.Split(','))
            .Select(parts => (int.Parse(parts[0]), int.Parse(parts[1])))
            .ToArray();
}
