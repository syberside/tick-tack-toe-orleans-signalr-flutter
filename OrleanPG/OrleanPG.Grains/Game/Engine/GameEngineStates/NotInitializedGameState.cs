using OrleanPG.Grains.Game.Engine.Actions;
using System;

namespace OrleanPG.Grains.Game.Engine.GameEngineStates
{
    internal class NotInitializedGameState : IGameEngineState
    {
        private readonly GameState _state;

        public NotInitializedGameState(GameState state) => _state = state;

        public GameState Process(UserTurnAction action) => throw new InvalidOperationException();

        public GameState Process(TimeOutAction action) => throw new InvalidOperationException();

        public GameState Process(InitializeAction action)
            => _state with { XPlayer = action.XPlayer, OPlayer = action.OPlayer };
    }
}
