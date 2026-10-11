using System.Collections.Concurrent;

namespace TicTacToe.Core;

/// <summary>
/// Loads and stores games and records completed games on the scoreboard.
/// Holds the per-game locks, so it must be a singleton alongside the repository and scoreboard.
/// </summary>
public sealed class GameService(IGameRepository repository, Scoreboard scoreboard)
{
    private readonly IGameRepository _repository = repository;
    private readonly Scoreboard _scoreboard = scoreboard;
    private readonly ConcurrentDictionary<Guid, Lock> _gameLocks = new();

    public Game CreateGame(GameMode mode)
    {
        var game = Game.Create(mode);
        _repository.Add(game);
        return game;
    }

    /// <summary>A read needs no lock: a stored game is an immutable snapshot.</summary>
    public Game? GetGame(Guid id) => _repository.Find(id);

    public GameResult MakeMove(Guid id, Player player, Position position) =>
        Mutate(id, game => game.Play(player, position));

    public GameResult Undo(Guid id) => Mutate(id, game => game.Undo());

    public GameResult Reset(Guid id) => Mutate(id, game => GameResult.Success(game.Reset()));

    public ScoreSnapshot GetScoreboard() => _scoreboard.Snapshot();

    public ScoreSnapshot ResetScoreboard()
    {
        _scoreboard.Reset();
        return _scoreboard.Snapshot();
    }

    /// <summary>
    /// Read → change → score → store under the game's lock (SPEC I-7), so two concurrent requests can never
    /// both build on the same version or both score the same result.
    /// </summary>
    private GameResult Mutate(Guid id, Func<Game, GameResult> change)
    {
        // Check first so unknown ids do not each leave a lock behind.
        if (_repository.Find(id) is null)
        {
            return GameResult.Failure(GameError.GameNotFound);
        }

        lock (_gameLocks.GetOrAdd(id, _ => new Lock()))
        {
            // Re-read under the lock: another request may have stored a newer version since the check above.
            if (_repository.Find(id) is not { } before)
            {
                return GameResult.Failure(GameError.GameNotFound);
            }

            var result = change(before);
            if (result.Game is { } after)
            {
                // Score exactly once, on the InProgress → Won/Draw transition (I-6).
                if (before.Status == GameStatus.InProgress && after.Status != GameStatus.InProgress)
                {
                    _scoreboard.Record(after);
                }

                _repository.Update(after);
            }

            return result;
        }
    }
}
