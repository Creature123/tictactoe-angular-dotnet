namespace TicTacToe.Core;

/// <summary>Loads and stores games and records completed games on the scoreboard.</summary>
public sealed class GameService(IGameRepository repository, Scoreboard scoreboard)
{
    private readonly IGameRepository _repository = repository;
    private readonly Scoreboard _scoreboard = scoreboard;

    public Game CreateGame(GameMode mode) => throw new NotImplementedException();

    public Game? GetGame(Guid id) => throw new NotImplementedException();

    public GameResult MakeMove(Guid id, Player player, Position position) => throw new NotImplementedException();

    public GameResult Undo(Guid id) => throw new NotImplementedException();

    public GameResult Reset(Guid id) => throw new NotImplementedException();

    public ScoreSnapshot GetScoreboard() => throw new NotImplementedException();

    public ScoreSnapshot ResetScoreboard() => throw new NotImplementedException();
}
