using System.Net;
using System.Net.Http.Json;
using TicTacToe.Api.Contracts;
using TicTacToe.Core;

namespace TicTacToe.Api.Tests;

public sealed class ScoreboardEndpointTests : ApiTestBase
{
    [Fact]
    [Trait("Req", "MH-08")]
    public async Task GetScoreboard_FreshHost_IsAllZero()
    {
        var score = await GetScoreboardAsync();

        Assert.Equal(new ScoreboardResponse(0, 0, 0), score);
    }

    [Fact]
    [Trait("Req", "FR-14"), Trait("Req", "MH-09")]
    public async Task Win_CountsExactlyOnce_EvenAfterMoreRequests()
    {
        var game = await CreateGameAsync(GameMode.TwoPlayer);
        await PlayAllAsync(game.Id, (0, 0), (1, 0), (0, 1), (1, 1), (0, 2));

        // None of these may change the count of a game that is already scored.
        (await PostMoveAsync(game.Id, "O", 2, 2)).Dispose();
        (await Client.PostAsync($"/api/games/{game.Id}/undo", null)).Dispose();
        await GetGameAsync(game.Id);

        Assert.Equal(new ScoreboardResponse(1, 0, 0), await GetScoreboardAsync());
    }

    [Fact]
    [Trait("Req", "MH-10")]
    public async Task ResetScoreboard_ZeroesCountsAndLeavesGameUnchanged()
    {
        var game = await CreateGameAsync(GameMode.TwoPlayer);
        var won = await PlayAllAsync(game.Id, (0, 0), (1, 0), (0, 1), (1, 1), (0, 2));

        using var response = await Client.PostAsync("/api/scoreboard/reset", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(new ScoreboardResponse(0, 0, 0), await response.Content.ReadFromJsonAsync<ScoreboardResponse>(Json));
        var after = await GetGameAsync(game.Id);
        Assert.Equal(won.Moves, after.Moves);
        Assert.Equal(GameStatus.Won, after.Status);
    }
}
