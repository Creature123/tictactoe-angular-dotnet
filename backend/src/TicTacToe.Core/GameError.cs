namespace TicTacToe.Core;

/// <summary>Expected rule violations. Returned as results, never thrown (SPEC §4).</summary>
public enum GameError
{
    OutOfBounds,
    GameCompleted,
    NotYourTurn,
    CellOccupied,
    NothingToUndo,
    GameNotFound,
}
