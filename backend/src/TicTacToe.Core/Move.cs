namespace TicTacToe.Core;

/// <summary>One played move. <see cref="Number"/> is 1-based.</summary>
public sealed record Move(int Number, Player Player, Position Position);
