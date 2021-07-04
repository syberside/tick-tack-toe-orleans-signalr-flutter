using OrleanPG.Grains.Game.Engine.Actions;
using System;

namespace OrleanPG.Grains.Game.Engine.GameEngineStates
{
    internal class CompletedGameState : IGameEngineState
    {
        private readonly GameState _state;

        public CompletedGameState(GameState state) => _state = state;

        public GameState Process(UserTurnAction action)
        {
            throw new InvalidOperationException();
        }

        public GameState Process(TimeOutAction action) => _state;
    }
}
