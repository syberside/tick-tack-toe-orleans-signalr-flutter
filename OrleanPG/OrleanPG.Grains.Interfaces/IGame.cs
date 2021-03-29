using Orleans;
using System;
using System.Threading.Tasks;

namespace OrleanPG.Grains.Interfaces
{
    public interface IGame : IObservableGame, IGrainWithGuidKey
    {
        Task<GameStatusDto> TurnAsync(int x, int y, AuthorizationToken player);
    }
}