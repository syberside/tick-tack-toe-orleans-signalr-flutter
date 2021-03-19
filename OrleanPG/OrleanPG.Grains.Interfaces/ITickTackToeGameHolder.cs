using Orleans;
using System;
using System.Threading.Tasks;

namespace OrleanPG.Grains.Interfaces
{
    public interface ITickTackToeGameHolder: IGrainWithIntegerKey
    {
        Task<string> MakeATurn(int x, int y, Guid userId);
    }
}
