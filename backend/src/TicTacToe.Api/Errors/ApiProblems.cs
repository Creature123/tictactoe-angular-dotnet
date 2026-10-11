using Microsoft.AspNetCore.Mvc;
using TicTacToe.Core;

namespace TicTacToe.Api.Errors;

/// <summary>
/// The single place where errors become RFC 7807 ProblemDetails with an <c>errorCode</c> extension (BE-10).
/// <c>detail</c> is shown to the player as-is (FE-07), so positions in it are 1-based.
/// </summary>
public static class ApiProblems
{
    public static ObjectResult For(GameError error, Position? position = null) => error switch
    {
        GameError.OutOfBounds => Create(StatusCodes.Status400BadRequest, "Out of bounds", ErrorCode(error),
            "Row and column must each be between 1 and 3."),
        GameError.GameNotFound => Create(StatusCodes.Status404NotFound, "Game not found", ErrorCode(error),
            "No game exists with that id. Start a new game."),
        GameError.GameCompleted => Create(StatusCodes.Status409Conflict, "Game completed", ErrorCode(error),
            "This game is over. Reset it or start a new game."),
        GameError.NotYourTurn => Create(StatusCodes.Status409Conflict, "Not your turn", ErrorCode(error),
            "It is not your turn."),
        GameError.CellOccupied => Create(StatusCodes.Status409Conflict, "Cell occupied", ErrorCode(error),
            position is { } p ? $"Row {p.Row + 1}, Column {p.Col + 1} is already taken." : "That cell is already taken."),
        GameError.NothingToUndo => Create(StatusCodes.Status409Conflict, "Nothing to undo", ErrorCode(error),
            "There is no move to undo."),
        _ => throw new ArgumentOutOfRangeException(nameof(error), error, "Unmapped game error."),
    };

    /// <summary>The wire <c>errorCode</c> for a domain error; also used as the structured log field.</summary>
    public static string ErrorCode(GameError error) => error switch
    {
        GameError.OutOfBounds => "OUT_OF_BOUNDS",
        GameError.GameNotFound => "GAME_NOT_FOUND",
        GameError.GameCompleted => "GAME_COMPLETED",
        GameError.NotYourTurn => "NOT_YOUR_TURN",
        GameError.CellOccupied => "CELL_OCCUPIED",
        GameError.NothingToUndo => "NOTHING_TO_UNDO",
        _ => throw new ArgumentOutOfRangeException(nameof(error), error, "Unmapped game error."),
    };

    /// <summary>Malformed body: missing field, unknown enum value or wrong type (A-11).</summary>
    public static ObjectResult InvalidRequest() => Create(StatusCodes.Status400BadRequest, "Invalid request",
        "INVALID_REQUEST", "The request is missing a field or has an invalid value.");

    private static ObjectResult Create(int status, string title, string errorCode, string detail)
    {
        var problem = new ProblemDetails
        {
            Type = "about:blank",
            Title = title,
            Status = status,
            Detail = detail,
            Extensions = { ["errorCode"] = errorCode },
        };

        return new ObjectResult(problem)
        {
            StatusCode = status,
            ContentTypes = { "application/problem+json" },
        };
    }
}
