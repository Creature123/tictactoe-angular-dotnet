using System.Net;
using System.Net.Http.Json;
using TicTacToe.Core;

namespace TicTacToe.Api.Tests;

/// <summary>One test per errorCode (BE-04 … BE-10). Every move error must leave the game unchanged (FR-08).</summary>
public sealed class GameErrorTests : ApiTestBase
{
    [Fact]
    [Trait("Req", "BE-05")]
    public async Task Move_OutsideBoard_Returns400OutOfBounds()
    {
        var game = await CreateGameAsync(GameMode.TwoPlayer);

        using var response = await PostMoveAsync(game.Id, "X", 3, 0);

        await AssertProblemAsync(response, HttpStatusCode.BadRequest, "OUT_OF_BOUNDS");
        Assert.Empty((await GetGameAsync(game.Id)).Moves);
    }

    [Theory]
    [InlineData("""{ "player": "X", "col": 0 }""")]
    [InlineData("""{ "player": "Z", "row": 0, "col": 0 }""")]
    [InlineData("""{ "player": 0, "row": 0, "col": 0 }""")]
    [InlineData("""{ "player": "X", "row": "a", "col": 0 }""")]
    [InlineData("")]
    [Trait("Req", "BE-04")]
    public async Task Move_MalformedBody_Returns400InvalidRequest(string body)
    {
        var game = await CreateGameAsync(GameMode.TwoPlayer);

        using var content = new StringContent(body, System.Text.Encoding.UTF8, "application/json");
        using var response = await Client.PostAsync($"/api/games/{game.Id}/moves", content);

        await AssertProblemAsync(response, HttpStatusCode.BadRequest, "INVALID_REQUEST");
        Assert.Empty((await GetGameAsync(game.Id)).Moves);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("""{ "mode": "Solo" }""")]
    [Trait("Req", "BE-04")]
    public async Task CreateGame_MissingOrUnknownMode_Returns400InvalidRequest(string body)
    {
        using var content = new StringContent(body, System.Text.Encoding.UTF8, "application/json");
        using var response = await Client.PostAsync("/api/games", content);

        await AssertProblemAsync(response, HttpStatusCode.BadRequest, "INVALID_REQUEST");
    }

    [Theory]
    [InlineData("GET", "")]
    [InlineData("POST", "/moves")]
    [InlineData("POST", "/undo")]
    [InlineData("POST", "/reset")]
    [Trait("Req", "BE-09")]
    public async Task UnknownGameId_Returns404GameNotFound(string method, string suffix)
    {
        using var request = new HttpRequestMessage(new HttpMethod(method), $"/api/games/{Guid.NewGuid()}{suffix}");
        if (suffix == "/moves")
        {
            request.Content = JsonContent.Create(new { player = "X", row = 0, col = 0 });
        }

        using var response = await Client.SendAsync(request);

        await AssertProblemAsync(response, HttpStatusCode.NotFound, "GAME_NOT_FOUND");
    }

    [Fact]
    [Trait("Req", "BE-09")]
    public async Task NonGuidId_Returns404GameNotFound()
    {
        using var response = await Client.GetAsync("/api/games/not-a-guid");

        await AssertProblemAsync(response, HttpStatusCode.NotFound, "GAME_NOT_FOUND");
    }

    [Fact]
    [Trait("Req", "BE-09")]
    public async Task UnknownGameId_WithMalformedBody_Returns404First()
    {
        using var content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json");
        using var response = await Client.PostAsync($"/api/games/{Guid.NewGuid()}/moves", content);

        await AssertProblemAsync(response, HttpStatusCode.NotFound, "GAME_NOT_FOUND");
    }

    [Fact]
    [Trait("Req", "FR-04"), Trait("Req", "BE-06"), Trait("Req", "FR-08")]
    public async Task Move_OnOccupiedCell_Returns409CellOccupied()
    {
        var game = await CreateGameAsync(GameMode.TwoPlayer);
        var before = await PlayAsync(game.Id, Player.X, 0, 0);

        using var response = await PostMoveAsync(game.Id, "O", 0, 0);

        var problem = await AssertProblemAsync(response, HttpStatusCode.Conflict, "CELL_OCCUPIED");
        Assert.Equal("Row 1, Column 1 is already taken.", problem.Detail);
        var after = await GetGameAsync(game.Id);
        Assert.Equal(before.Moves, after.Moves);
        Assert.Equal(Player.O, after.CurrentPlayer);
    }

    [Fact]
    [Trait("Req", "BE-08"), Trait("Req", "FR-08")]
    public async Task Move_ByWrongPlayer_Returns409NotYourTurn()
    {
        var game = await CreateGameAsync(GameMode.TwoPlayer);

        using var response = await PostMoveAsync(game.Id, "O", 0, 0);

        await AssertProblemAsync(response, HttpStatusCode.Conflict, "NOT_YOUR_TURN");
        var after = await GetGameAsync(game.Id);
        Assert.Empty(after.Moves);
        Assert.Equal(Player.X, after.CurrentPlayer);
    }

    [Fact]
    [Trait("Req", "FR-13"), Trait("Req", "BE-07")]
    public async Task Move_AfterWin_Returns409GameCompleted()
    {
        var game = await CreateGameAsync(GameMode.TwoPlayer);
        var won = await PlayAllAsync(game.Id, (0, 0), (1, 0), (0, 1), (1, 1), (0, 2));

        using var response = await PostMoveAsync(game.Id, "O", 2, 2);

        await AssertProblemAsync(response, HttpStatusCode.Conflict, "GAME_COMPLETED");
        Assert.Equal(won.Moves, (await GetGameAsync(game.Id)).Moves);
    }

    [Fact]
    [Trait("Req", "MH-07")]
    public async Task Undo_AfterWin_Returns409GameCompletedAndKeepsScore()
    {
        var game = await CreateGameAsync(GameMode.TwoPlayer);
        await PlayAllAsync(game.Id, (0, 0), (1, 0), (0, 1), (1, 1), (0, 2));

        using var response = await Client.PostAsync($"/api/games/{game.Id}/undo", null);

        await AssertProblemAsync(response, HttpStatusCode.Conflict, "GAME_COMPLETED");
        Assert.Equal(1, (await GetScoreboardAsync()).XWins);
    }

    [Fact]
    [Trait("Req", "MH-06")]
    public async Task Undo_WithNoMoves_Returns409NothingToUndo()
    {
        var game = await CreateGameAsync(GameMode.TwoPlayer);

        using var response = await Client.PostAsync($"/api/games/{game.Id}/undo", null);

        await AssertProblemAsync(response, HttpStatusCode.Conflict, "NOTHING_TO_UNDO");
    }
}
