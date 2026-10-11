using System.Net;
using TicTacToe.Api.Contracts;
using TicTacToe.Core;

namespace TicTacToe.Api.Tests;

public sealed class ComputerModeTests : ApiTestBase
{
    /// <summary>
    /// A full deterministic game against the computer (SPEC §6): center, block, block, corner, then X's 9th move
    /// draws and the computer does not reply. The draw is scored once and the finished game cannot be undone.
    /// </summary>
    [Fact]
    [Trait("Req", "MH-13"), Trait("Req", "MH-15"), Trait("Req", "FR-17"), Trait("Req", "T-08")]
    public async Task FullRoundTrip_EndsInDrawWithoutComputerReply()
    {
        var game = await CreateGameAsync(GameMode.VsComputer);

        // (X row, X col) → the computer's reply as (row, col), or null when X's move ends the game.
        (int Row, int Col, (int Row, int Col)? Reply)[] turns =
        [
            (0, 0, (1, 1)), // X0 → O4 center
            (0, 1, (0, 2)), // X1 → O2 blocks 0-1-2
            (2, 0, (1, 0)), // X6 → O3 blocks 0-3-6
            (1, 2, (2, 2)), // X5 → no win/block/center, O8 first free corner
            (2, 1, null),   // X7 → 9th move, draw, no reply
        ];

        GameStateResponse state = game;
        foreach (var (row, col, reply) in turns)
        {
            var movesBefore = state.Moves.Count;
            state = await PlayAsync(game.Id, Player.X, row, col);

            Assert.Equal(new MoveResponse(movesBefore + 1, Player.X, row, col), state.Moves[movesBefore]);
            if (reply is { } o)
            {
                Assert.Equal(movesBefore + 2, state.Moves.Count);
                Assert.Equal(new MoveResponse(movesBefore + 2, Player.O, o.Row, o.Col), state.Moves[^1]);
                Assert.Equal(Player.X, state.CurrentPlayer);
            }
            else
            {
                Assert.Equal(movesBefore + 1, state.Moves.Count);
            }
        }

        Assert.Equal(GameStatus.Draw, state.Status);
        Assert.Null(state.Winner);
        Assert.Null(state.CurrentPlayer);
        Assert.Empty(state.WinningCells);
        Assert.Equal(new ScoreboardResponse(0, 0, 1), state.Scoreboard);

        using var undo = await Client.PostAsync($"/api/games/{game.Id}/undo", null);
        await AssertProblemAsync(undo, HttpStatusCode.Conflict, "GAME_COMPLETED");
        Assert.Equal(new ScoreboardResponse(0, 0, 1), await GetScoreboardAsync());
    }

    [Fact]
    [Trait("Req", "MH-04")]
    public async Task Undo_RemovesComputerReplyAndHumanMove()
    {
        var game = await CreateGameAsync(GameMode.VsComputer);
        await PlayAsync(game.Id, Player.X, 0, 0);

        using var response = await Client.PostAsync($"/api/games/{game.Id}/undo", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var state = await ReadGameAsync(response);
        Assert.Empty(state.Moves);
        Assert.All(state.Board, Assert.Null);
        Assert.Equal(Player.X, state.CurrentPlayer);
    }

    [Fact]
    [Trait("Req", "BE-08")]
    public async Task Move_AsO_Returns409NotYourTurn()
    {
        var game = await CreateGameAsync(GameMode.VsComputer);

        using var response = await PostMoveAsync(game.Id, "O", 0, 0);

        await AssertProblemAsync(response, HttpStatusCode.Conflict, "NOT_YOUR_TURN");
    }
}
