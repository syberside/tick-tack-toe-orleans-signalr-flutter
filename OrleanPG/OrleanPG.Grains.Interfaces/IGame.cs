using Orleans;
using System;
using System.Threading.Tasks;

namespace OrleanPG.Grains.Interfaces
{
    public interface IGame : IGrainWithStringKey
    {
        Task<GameStatus> TurnAsync(int x, int y, AuthorizationToken player);
    }

    public interface IGameInitializer : IGrainWithStringKey
    {
        Task StartAsync(AuthorizationToken playerX, AuthorizationToken playerO);
    }
}
