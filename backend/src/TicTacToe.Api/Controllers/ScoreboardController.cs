using Microsoft.AspNetCore.Mvc;
using TicTacToe.Api.Contracts;
using TicTacToe.Core;

namespace TicTacToe.Api.Controllers;

[ApiController]
[Route("api/scoreboard")]
public sealed class ScoreboardController(GameService games) : ControllerBase
{
    private readonly GameService _games = games;

    /// <summary>E6: the session scoreboard (one per API process, A-01).</summary>
    [HttpGet]
    [ProducesResponseType<ScoreboardResponse>(StatusCodes.Status200OK)]
    public ScoreboardResponse Get() => ScoreboardResponse.From(_games.GetScoreboard());

    /// <summary>E7: sets every count to zero; games are not touched.</summary>
    [HttpPost("reset")]
    [ProducesResponseType<ScoreboardResponse>(StatusCodes.Status200OK)]
    public ScoreboardResponse Reset() => ScoreboardResponse.From(_games.ResetScoreboard());
}
