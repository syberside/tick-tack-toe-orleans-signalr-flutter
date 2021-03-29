using System.Threading.Tasks;

namespace OrleanPG.Grains.Interfaces
{
    public interface IObservableGame
    {
        Task<GameStatusDto> GetStatus();

        Task<GameStatusDto> SubscribeToUpdatesOrMarkAlive(IGameObserver observer);

        Task UnsubscribeFromUpdates(IGameObserver observer);
    }
}