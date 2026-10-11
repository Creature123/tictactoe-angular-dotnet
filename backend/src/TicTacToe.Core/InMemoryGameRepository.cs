using System.Collections.Concurrent;

namespace TicTacToe.Core;

/// <summary>
/// Keeps games in memory until the process restarts (A-13). Games are immutable snapshots, so readers never
/// see a half-applied move. Callers serialize writes to one game with that game's lock (see GameService).
/// </summary>
public sealed class InMemoryGameRepository : IGameRepository
{
    private readonly ConcurrentDictionary<Guid, Game> _games = new();

    public void Add(Game game)
    {
        if (!_games.TryAdd(game.Id, game))
        {
            throw new InvalidOperationException($"Game {game.Id} already exists.");
        }
    }

    public Game? Find(Guid id) => _games.GetValueOrDefault(id);

    public void Update(Game game) => _games[game.Id] = game;
}
