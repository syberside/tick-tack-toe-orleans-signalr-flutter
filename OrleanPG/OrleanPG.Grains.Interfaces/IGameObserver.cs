using Orleans;

namespace OrleanPG.Grains.Interfaces
{
    public interface IGameObserver : IGrainObserver
    {
        void GameStateUpdated(GameStatusDto newState);
    }
}