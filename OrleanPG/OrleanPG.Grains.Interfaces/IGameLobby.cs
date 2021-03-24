using Orleans;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OrleanPG.Grains.Interfaces
{
    public interface IGameLobby : IGrainWithGuidKey
    {
        Task<AuthorizationToken> AuthorizeAsync(string username);
        Task<GameListItemDto[]> FindGamesAsync();

        Task JoinGameAsync(AuthorizationToken authToken, GameId id);

        Task<GameId> CreateNewAsync(AuthorizationToken authToken, bool playForX);
    }
}
