using System;
using System.Threading.Tasks;

namespace OrleanPG.Grains.Game.GrainLogic
{
    public class DelegatesToGameStateStoreAdapter : IGameStateStore
    {
        private readonly Func<GameState> _getState;
        private readonly Func<GameState, Task> _writeState;

        public DelegatesToGameStateStoreAdapter(Func<GameState> getState, Func<GameState, Task> writeState)
        {
            _getState = getState;
            _writeState = writeState;
        }

        public GameState GetState() => _getState();

        public Task WriteState(GameState newState) => _writeState(newState);
    }
}

