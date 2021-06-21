using Orleans;
using System.Threading.Tasks;

namespace OrleanPG.Grains.Interfaces
{

    /// <summary>
    /// TODO: Split games and players logic to separate grains?
    /// </summary>
    public interface IGameLobby : IGrainWithGuidKey
    {
        Task<AuthorizationToken> AuthorizeAsync(string username);
        Task<GameListItemDto[]> FindGamesAsync();

        Task<GameStatusDto> JoinGameAsync(AuthorizationToken authToken, GameId id);

        Task<GameId> CreateGameAsync(AuthorizationToken authToken, bool playForX);
        Task AddBotAsync(AuthorizationToken owner, GameId gameId);
        Task<string?[]> ResolveUserNamesAsync(params AuthorizationToken?[] tokens);
    }
}
