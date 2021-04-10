using OrleanPG.Grains.Interfaces;

namespace OrleanPG.Grains.Game
{
    public record GameStorageData(AuthorizationToken? XPlayer, AuthorizationToken? OPlayer, GameState Status, GameMap Map)
    {
        public GameStorageData() : this(null, null, GameState.XTurn, new()) { }

        public bool IsInitialized => XPlayer != null && OPlayer != null;

    }
}
