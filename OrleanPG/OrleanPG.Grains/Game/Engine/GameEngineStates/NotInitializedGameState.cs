using OrleanPG.Grains.Game.Engine.Actions;
using System;

namespace OrleanPG.Grains.Game.Engine.GameEngineStates
{
    internal class NotInitializedGameState : IGameEngineState
    {
        public GameState Process(UserTurnAction action)
        {
            throw new InvalidOperationException();
        }

        public GameState Process(TimeOutAction action)
        {
            throw new InvalidOperationException();
        }
    }
}
