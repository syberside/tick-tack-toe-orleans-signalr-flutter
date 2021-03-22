using Orleans;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OrleanPG.Grains.Interfaces
{
    public interface IGameLobby : IGrainWithIntegerKey
    {
        Task<AuthorizationToken> AuthorizeAsync(string username);
        Task<GameGeneralInfo[]> FindGamesAsync();

        Task JoinGameAsync(AuthorizationToken authToken, GameId id);

        Task<GameId> CreateNewAsync(AuthorizationToken authToken, bool playForX);
    }
}
