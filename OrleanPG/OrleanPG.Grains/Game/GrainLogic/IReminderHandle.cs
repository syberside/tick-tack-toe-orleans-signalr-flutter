using System.Threading.Tasks;

namespace OrleanPG.Grains.Game.GrainLogic
{
    public interface IReminderHandle
    {
        Task Set();
        Task Reset();
    }
}

