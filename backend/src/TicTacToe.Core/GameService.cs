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

    public GameUpdate MakeMove(Guid id, Player player, Position position) =>
        Mutate(id, game => game.Play(player, position));

    public GameUpdate Undo(Guid id) => Mutate(id, game => game.Undo());

    public GameUpdate Reset(Guid id) => Mutate(id, game => GameResult.Success(game.Reset()));

    public ScoreSnapshot GetScoreboard() => _scoreboard.Snapshot();

    public ScoreSnapshot ResetScoreboard()
    {
        _scoreboard.Reset();
        return _scoreboard.Snapshot();
    }

    /// <summary>
    /// Read → change → score → store under the game's lock (SPEC I-7), so two concurrent requests can never
    /// both build on the same version or both score the same result. The returned scoreboard is read under the
    /// same lock, so it always matches the returned game.
    /// </summary>
    private GameUpdate Mutate(Guid id, Func<Game, GameResult> change)
    {
        // Check first so unknown ids do not each leave a lock behind.
        if (_repository.Find(id) is null)
        {
            return new GameUpdate(null, GameError.GameNotFound, _scoreboard.Snapshot());
        }

        lock (_gameLocks.GetOrAdd(id, _ => new Lock()))
        {
            // Re-read under the lock: another request may have stored a newer version since the check above.
            if (_repository.Find(id) is not { } before)
            {
                return new GameUpdate(null, GameError.GameNotFound, _scoreboard.Snapshot());
            }

            var result = change(before);
            if (result.Game is not { } after)
            {
                return new GameUpdate(null, result.Error, _scoreboard.Snapshot());
            }

            // Score exactly once, on the InProgress → Won/Draw transition (I-6). Record returns the counts that
            // include this game, even if the scoreboard is reset straight afterwards.
            var score = before.Status == GameStatus.InProgress && after.Status != GameStatus.InProgress
                ? _scoreboard.Record(after)
                : _scoreboard.Snapshot();

            _repository.Update(after);
            return new GameUpdate(after, null, score);
        }
    }
}
