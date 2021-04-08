using OrleanPG.Grains.Interfaces;

namespace OrleanPG.Grains.GameBot
{
    public record GameBotStorageData(AuthorizationToken? Token, bool PlayForX)
    {
        public GameBotStorageData() : this(null, false) { }
    }

}
