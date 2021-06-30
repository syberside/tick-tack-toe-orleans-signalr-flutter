using Orleans;
using System.Threading.Tasks;

namespace OrleanPG.Grains.Interfaces
{
    public interface IGame : IGrainWithGuidKey
    {
        Task<GameStatusDto> TurnAsync(GameMapPoint position, AuthorizationToken player);

        Task<GameStatusDto> GetStatus();
    }
}