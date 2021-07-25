using System.Threading.Tasks;

namespace OrleanPG.Grains.Game.GrainLogic
{
    public interface IGameStateStore
    {
        public GameState GetState();

        public Task WriteState(GameState newState);
    }
}

