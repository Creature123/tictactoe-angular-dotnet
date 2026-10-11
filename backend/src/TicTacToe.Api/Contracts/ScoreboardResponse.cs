using TicTacToe.Core;

namespace TicTacToe.Api.Contracts;

public sealed record ScoreboardResponse(int XWins, int OWins, int Draws)
{
    public static ScoreboardResponse From(ScoreSnapshot score) => new(score.XWins, score.OWins, score.Draws);
}
