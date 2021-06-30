
using OrleanPG.Grains.Interfaces;

namespace OrleanPG.Grains.Game
{
    public record GameState(AuthorizationToken? XPlayer, AuthorizationToken? OPlayer, GameStatus Status, GameMap Map)
    {
        public GameState() : this(null, null, GameStatus.XTurn, new()) { }

        public bool IsInitialized => XPlayer != null && OPlayer != null;

    }
}
