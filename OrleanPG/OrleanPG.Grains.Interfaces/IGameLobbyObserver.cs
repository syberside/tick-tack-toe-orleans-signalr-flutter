using Orleans;

namespace OrleanPG.Grains.Interfaces
{
    public interface IGameLobbyObserver : IGrainObserver
    {
        void NewGameCreated(GameId gameId, string username, bool isCreatorPlayingForX);
    }
}