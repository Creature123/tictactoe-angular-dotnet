using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using TicTacToe.Api.Contracts;
using TicTacToe.Core;

namespace TicTacToe.Api.Tests;

/// <summary>One happy path per /api/games endpoint (BE-01).</summary>
public sealed class GamesEndpointTests : ApiTestBase
{
    [Theory]
    [InlineData(GameMode.TwoPlayer)]
    [InlineData(GameMode.VsComputer)]
    [Trait("Req", "FR-01"), Trait("Req", "FR-05"), Trait("Req", "MH-11"), Trait("Req", "BE-03")]
    public async Task CreateGame_ReturnsCreatedWithEmptyBoardAndLocation(GameMode mode)
    {
        using var response = await Client.PostAsJsonAsync("/api/games", new { mode = mode.ToString() });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var game = await ReadGameAsync(response);
        Assert.Equal($"/api/games/{game.Id}", response.Headers.Location?.AbsolutePath);
        Assert.Equal(mode, game.Mode);
        Assert.Equal(GameStatus.InProgress, game.Status);
        Assert.Equal(Player.X, game.CurrentPlayer);
        Assert.Null(game.Winner);
        Assert.Empty(game.WinningCells);
        Assert.Equal(9, game.Board.Count);
        Assert.All(game.Board, Assert.Null);
        Assert.Empty(game.Moves);
        Assert.False(game.CanUndo);
        Assert.Equal(new ScoreboardResponse(0, 0, 0), game.Scoreboard);
    }

    [Fact]
    [Trait("Req", "BE-02")]
    public async Task GetGame_ReturnsServerStateIncludingEarlierMove()
    {
        var created = await CreateGameAsync(GameMode.TwoPlayer);
        await PlayAsync(created.Id, Player.X, 1, 2);

        var game = await GetGameAsync(created.Id);

        Assert.Equal(created.Id, game.Id);
        Assert.Equal([new MoveResponse(1, Player.X, 1, 2)], game.Moves);
        Assert.Equal(Player.X, game.Board[5]);
    }

    [Fact]
    [Trait("Req", "FR-02"), Trait("Req", "FR-07"), Trait("Req", "MH-02"), Trait("Req", "MH-12")]
    public async Task MakeMove_TwoPlayer_PlacesMarkAndPassesTurn()
    {
        var created = await CreateGameAsync(GameMode.TwoPlayer);

        var game = await PlayAsync(created.Id, Player.X, 0, 0);

        Assert.Equal(Player.X, game.Board[0]);
        Assert.Equal(Player.O, game.CurrentPlayer);
        Assert.Equal([new MoveResponse(1, Player.X, 0, 0)], game.Moves);
        Assert.True(game.CanUndo);
    }

    [Fact]
    [Trait("Req", "MH-03")]
    public async Task Undo_TwoPlayer_RemovesOnlyTheLastMove()
    {
        var created = await CreateGameAsync(GameMode.TwoPlayer);
        await PlayAllAsync(created.Id, (0, 0), (1, 1));

        using var response = await Client.PostAsync($"/api/games/{created.Id}/undo", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var game = await ReadGameAsync(response);
        Assert.Equal([new MoveResponse(1, Player.X, 0, 0)], game.Moves);
        Assert.Null(game.Board[4]);
        Assert.Equal(Player.O, game.CurrentPlayer);
        Assert.Equal(GameStatus.InProgress, game.Status);
    }

    [Fact]
    [Trait("Req", "FR-18")]
    public async Task Reset_AfterWin_ClearsSameGameAndKeepsScoreboard()
    {
        var created = await CreateGameAsync(GameMode.TwoPlayer);
        await PlayAllAsync(created.Id, (0, 0), (1, 0), (0, 1), (1, 1), (0, 2));

        using var response = await Client.PostAsync($"/api/games/{created.Id}/reset", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var game = await ReadGameAsync(response);
        Assert.Equal(created.Id, game.Id);
        Assert.Equal(GameMode.TwoPlayer, game.Mode);
        Assert.Empty(game.Moves);
        Assert.All(game.Board, Assert.Null);
        Assert.Equal(GameStatus.InProgress, game.Status);
        Assert.Equal(Player.X, game.CurrentPlayer);
        Assert.Null(game.Winner);
        Assert.Equal(new ScoreboardResponse(1, 0, 0), game.Scoreboard);
    }

    [Fact]
    [Trait("Req", "FR-09"), Trait("Req", "BE-11")]
    public async Task WinResponse_SerializesEnumsAsStringsAndHidesTurn()
    {
        var created = await CreateGameAsync(GameMode.TwoPlayer);
        await PlayAllAsync(created.Id, (0, 0), (1, 0), (0, 1), (1, 1));

        using var response = await PostMoveAsync(created.Id, "X", 0, 2);

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = body.RootElement;
        Assert.Equal("Won", root.GetProperty("status").GetString());
        Assert.Equal("X", root.GetProperty("winner").GetString());
        Assert.Equal("TwoPlayer", root.GetProperty("mode").GetString());
        Assert.Equal(JsonValueKind.Null, root.GetProperty("currentPlayer").ValueKind);
        Assert.Equal("[0,1,2]", root.GetProperty("winningCells").GetRawText());
        Assert.False(root.GetProperty("canUndo").GetBoolean());
    }
}
