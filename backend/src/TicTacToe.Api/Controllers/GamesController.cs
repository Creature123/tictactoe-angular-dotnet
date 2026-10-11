using Microsoft.AspNetCore.Mvc;
using TicTacToe.Api.Contracts;
using TicTacToe.Api.Errors;
using TicTacToe.Core;

namespace TicTacToe.Api.Controllers;

/// <summary>
/// Thin HTTP layer over <see cref="GameService"/>: bind → call → map to DTO. No game rules live here.
/// Actions are synchronous because the work is in-memory and CPU-bound.
/// </summary>
[ApiController]
[Route("api/games")]
public sealed class GamesController(GameService games, ILogger<GamesController> logger) : ControllerBase
{
    private readonly GameService _games = games;
    private readonly ILogger<GamesController> _logger = logger;

    /// <summary>E1: creates a game in the requested mode.</summary>
    [HttpPost]
    [ProducesResponseType<GameStateResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public IActionResult Create(CreateGameRequest? request)
    {
        if (!ModelState.IsValid || request?.Mode is not { } mode)
        {
            return ApiProblems.InvalidRequest();
        }

        var game = _games.CreateGame(mode);
        return CreatedAtAction(nameof(Get), new { id = game.Id }, ToResponse(game));
    }

    /// <summary>E2: reads a game.</summary>
    [HttpGet("{id}")]
    [ProducesResponseType<GameStateResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public IActionResult Get(string id) =>
        TryParseId(id) is { } gameId && _games.GetGame(gameId) is { } game
            ? Ok(ToResponse(game))
            : ApiProblems.For(GameError.GameNotFound);

    /// <summary>E3: plays a move. In VsComputer mode the response already includes the computer's reply.</summary>
    [HttpPost("{id}/moves")]
    [ProducesResponseType<GameStateResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public IActionResult Move(string id, MoveRequest? request)
    {
        // Precedence A-10: an unknown game beats a malformed body.
        if (TryParseId(id) is not { } gameId || _games.GetGame(gameId) is null)
        {
            return ApiProblems.For(GameError.GameNotFound);
        }

        if (!ModelState.IsValid || request is not { Player: { } player, Row: { } row, Col: { } col })
        {
            return ApiProblems.InvalidRequest();
        }

        var position = new Position(row, col);
        return ToActionResult(gameId, _games.MakeMove(gameId, player, position), position);
    }

    /// <summary>E4: undoes the last move (TwoPlayer) or the last X/O pair (VsComputer).</summary>
    [HttpPost("{id}/undo")]
    [ProducesResponseType<GameStateResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public IActionResult Undo(string id) => Run(id, _games.Undo);

    /// <summary>E5: clears the moves of the same game; the scoreboard is untouched.</summary>
    [HttpPost("{id}/reset")]
    [ProducesResponseType<GameStateResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public IActionResult Reset(string id) => Run(id, _games.Reset);

    private IActionResult Run(string id, Func<Guid, GameUpdate> action) =>
        TryParseId(id) is { } gameId
            ? ToActionResult(gameId, action(gameId))
            : ApiProblems.For(GameError.GameNotFound);

    /// <summary>The one place where a domain result becomes a response; rejected actions are logged here.</summary>
    private IActionResult ToActionResult(Guid gameId, GameUpdate update, Position? position = null)
    {
        if (update.Game is { } game)
        {
            // Use the scoreboard read under the game lock, not a fresh read that a concurrent reset could change.
            return Ok(GameStateResponse.From(game, update.Scoreboard));
        }

        var error = update.Error ?? throw new InvalidOperationException("A failed result has no error.");
        _logger.LogInformation("Rejected game action for {GameId}: {ErrorCode}", gameId, ApiProblems.ErrorCode(error));
        return ApiProblems.For(error, position);
    }

    private GameStateResponse ToResponse(Game game) => GameStateResponse.From(game, _games.GetScoreboard());

    /// <summary>A non-Guid id cannot name a game, so it is a 404 rather than a 400 (A-14).</summary>
    private static Guid? TryParseId(string id) => Guid.TryParse(id, out var gameId) ? gameId : null;
}
