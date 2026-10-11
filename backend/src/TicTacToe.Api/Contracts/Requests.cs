using System.ComponentModel.DataAnnotations;
using TicTacToe.Core;

namespace TicTacToe.Api.Contracts;

/// <summary>Body of POST /api/games. <c>mode</c> is required (A-06).</summary>
public sealed record CreateGameRequest([Required] GameMode? Mode);

/// <summary>
/// Body of POST /api/games/{id}/moves. Fields are nullable so a missing one fails [Required] (BE-04).
/// Row/col bounds are a domain rule (OUT_OF_BOUNDS), so there is deliberately no [Range] here.
/// </summary>
public sealed record MoveRequest(
    [Required] Player? Player,
    [Required] int? Row,
    [Required] int? Col);
