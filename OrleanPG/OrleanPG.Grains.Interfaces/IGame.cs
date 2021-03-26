using Orleans;
using System;
using System.Threading.Tasks;

namespace OrleanPG.Grains.Interfaces
{
    public interface IGame : IGrainWithGuidKey
    {
        Task<GameStatusDto> TurnAsync(int x, int y, AuthorizationToken player);

        Task<GameMap> GetMapAsync();

        Task<GameStatusDto> SubscribeToUpdatesOrMarkAlive(IGameObserver observer);

        Task UnsubscribeFromUpdates(IGameObserver observer);
    }
}