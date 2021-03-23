using Orleans;
using System;
using System.Threading.Tasks;

namespace OrleanPG.Grains.Interfaces
{
    public interface IGame : IGrainWithGuidKey
    {
        Task<GameStatus> TurnAsync(int x, int y, AuthorizationToken player);
    }

    public interface IGameInitializer : IGrainWithGuidKey
    {
        Task StartAsync(AuthorizationToken playerX, AuthorizationToken playerO);
    }
}
