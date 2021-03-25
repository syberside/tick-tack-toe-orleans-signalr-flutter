using Orleans;
using System;
using System.Threading.Tasks;

namespace OrleanPG.Grains.Interfaces
{
    public interface IGame : IGrainWithGuidKey
    {
        Task<GameStatusDto> TurnAsync(int x, int y, AuthorizationToken player);

        Task<GameMap> GetMapAsync();
    }

    public interface IGameInitializer : IRemindable, IGrainWithGuidKey
    {
        Task StartAsync(AuthorizationToken playerX, AuthorizationToken playerO);
    }
}
