using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.Testing;
using TicTacToe.Api.Contracts;
using TicTacToe.Core;

namespace TicTacToe.Api.Tests;

/// <summary>
/// Isolation: xUnit creates a new instance of the test class for every test, and this constructor starts a new
/// in-memory host for that instance. Each test therefore gets its own singleton <see cref="Scoreboard"/> and game
/// repository, so scoreboard assertions are absolute and tests can run in parallel without seeing each other.
/// </summary>
public abstract class ApiTestBase : IDisposable
{
    protected static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly WebApplicationFactory<Program> _factory = new();

    protected ApiTestBase() => Client = _factory.CreateClient();

    protected HttpClient Client { get; }

    public void Dispose()
    {
        Client.Dispose();
        _factory.Dispose();
        GC.SuppressFinalize(this);
    }

    protected async Task<GameStateResponse> CreateGameAsync(GameMode mode)
    {
        using var response = await Client.PostAsJsonAsync("/api/games", new { mode = mode.ToString() });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return await ReadGameAsync(response);
    }

    protected Task<HttpResponseMessage> PostMoveAsync(Guid id, string player, int row, int col) =>
        Client.PostAsJsonAsync($"/api/games/{id}/moves", new { player, row, col });

    protected async Task<GameStateResponse> PlayAsync(Guid id, Player player, int row, int col)
    {
        using var response = await PostMoveAsync(id, player.ToString(), row, col);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await ReadGameAsync(response);
    }

    /// <summary>Plays the moves in order, alternating X and O starting with X (TwoPlayer).</summary>
    protected async Task<GameStateResponse> PlayAllAsync(Guid id, params (int Row, int Col)[] moves)
    {
        GameStateResponse? state = null;
        for (var i = 0; i < moves.Length; i++)
        {
            state = await PlayAsync(id, i % 2 == 0 ? Player.X : Player.O, moves[i].Row, moves[i].Col);
        }

        return state ?? throw new ArgumentException("At least one move is required.", nameof(moves));
    }

    protected async Task<GameStateResponse> GetGameAsync(Guid id)
    {
        using var response = await Client.GetAsync($"/api/games/{id}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await ReadGameAsync(response);
    }

    protected async Task<ScoreboardResponse> GetScoreboardAsync() =>
        await Client.GetFromJsonAsync<ScoreboardResponse>("/api/scoreboard", Json)
        ?? throw new InvalidOperationException("Empty scoreboard body.");

    protected static async Task<GameStateResponse> ReadGameAsync(HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<GameStateResponse>(Json)
        ?? throw new InvalidOperationException("Empty game body.");

    /// <summary>Asserts the status code and the full ProblemDetails shape (BE-10), and returns the body.</summary>
    protected static async Task<ProblemBody> AssertProblemAsync(
        HttpResponseMessage response, HttpStatusCode status, string errorCode)
    {
        Assert.Equal(status, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var problem = await response.Content.ReadFromJsonAsync<ProblemBody>(Json)
            ?? throw new InvalidOperationException("Empty problem body.");
        Assert.Equal(errorCode, problem.ErrorCode);
        Assert.Equal((int)status, problem.Status);
        Assert.False(string.IsNullOrWhiteSpace(problem.Title));
        Assert.False(string.IsNullOrWhiteSpace(problem.Detail));
        return problem;
    }

    protected sealed record ProblemBody(string? Type, string? Title, int Status, string? Detail, string? ErrorCode);
}
