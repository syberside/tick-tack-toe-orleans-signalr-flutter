using OrleanPG.Grains.Interfaces.Dtos;
using System.Collections.Generic;

namespace OrleanPG.Grains.GameLobbyGrain
{
    public record GamesStorageState(Dictionary<GameId, GameParticipation> RegisteredGames)
    {
        public GamesStorageState() : this(new Dictionary<GameId, GameParticipation>()) { }
    }
}
