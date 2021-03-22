using Orleans;
using System.Threading.Tasks;

namespace OrleanPG.Grains.Interfaces
{
    public interface IGameLobby : IGrainWithIntegerKey
    {
        Task<AuthorizationToken> AuthorizeAsync(string username);
        Task<GameGeneralInfo[]> FindGamesAsync();

        Task<GameToken> JoinGameAsync(AuthorizationToken authToken, GameId id);

        Task<CreateGameResult> CreateNewAsync(AuthorizationToken authToken, bool playForX);
    }
}
