namespace TicTacToe.Core;

public interface IGameRepository
{
    void Add(Game game);

    Game? Find(Guid id);

    void Update(Game game);
}
