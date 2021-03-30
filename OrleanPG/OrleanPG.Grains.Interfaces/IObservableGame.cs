using Orleans;
using System.Threading.Tasks;

namespace OrleanPG.Grains.Interfaces
{
    public interface IObservableGame : IGrainWithGuidKey
    {
        Task<GameStatusDto> GetStatus();

        Task<GameStatusDto> SubscribeAndMarkAlive(IGameObserver observer);

        Task UnsubscribeFromUpdates(IGameObserver observer);
    }
}