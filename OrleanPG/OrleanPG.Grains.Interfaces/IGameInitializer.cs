using Orleans;
using System.Threading.Tasks;

namespace OrleanPG.Grains.Interfaces
{
    public interface IGameInitializer : IRemindable, IGrainWithGuidKey
    {
        Task StartAsync(AuthorizationToken playerX, AuthorizationToken playerO);
    }
}
