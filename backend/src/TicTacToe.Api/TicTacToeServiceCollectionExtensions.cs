using TicTacToe.Core;

namespace TicTacToe.Api;

public static class TicTacToeServiceCollectionExtensions
{
    /// <summary>
    /// Registers the game domain. Everything is a singleton on purpose: games and the scoreboard live in memory
    /// for the lifetime of the process (A-01, A-13), and <see cref="GameService"/> holds the per-game locks (I-7).
    /// </summary>
    public static IServiceCollection AddTicTacToe(this IServiceCollection services)
    {
        services.AddSingleton<IGameRepository, InMemoryGameRepository>();
        services.AddSingleton<Scoreboard>();
        services.AddSingleton<GameService>();

        return services;
    }
}
