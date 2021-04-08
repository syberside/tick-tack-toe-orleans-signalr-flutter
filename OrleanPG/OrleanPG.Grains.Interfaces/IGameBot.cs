using Orleans;
using System.Threading.Tasks;

namespace OrleanPG.Grains.Interfaces
{
    public interface IGameBot : IGrainWithGuidKey
    {
        Task InitAsync(AuthorizationToken token, bool playForX);
    }
}
