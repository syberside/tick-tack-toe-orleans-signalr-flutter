using OrleanPG.Grains.Game.Engine.Actions;
using OrleanPG.Grains.Game.Engine.GameEngineStates;
using OrleanPG.Grains.Game.Engine.WinCheckers;
using OrleanPG.Grains.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OrleanPG.Grains.Game.Engine
{
    public class GameEngine : IGameEngine
    {
        private readonly IWinChecker[] _winCheckers;

        public GameEngine(IEnumerable<IWinChecker> winCheckers)
        {
            _winCheckers = winCheckers.ToArray();
        }

        public GameState Process<TAction>(TAction action, GameState state) where TAction : IGameAction
        {
            var engineState = GetEngineState(state);
            return action switch
            {
                InitializeAction initialize => engineState.Process(initialize),
                UserTurnAction userTurn => engineState.Process(userTurn),
                TimeOutAction timeout => engineState.Process(timeout),
                _ => throw new NotSupportedException($"Action {action?.GetType()} is not supported"),
            };
        }

        private IGameEngineState GetEngineState(GameState state)
        {
            if (state.XPlayer == null && state.OPlayer == null)
            {
                return new NotInitializedGameState(state);
            }
            else if (state.Status.IsEndStatus())
            {
                return new CompletedGameState(state);
            }
            else
            {
                var initializedState = new InProgressGameState.InitializedGameState(state);
                return new InProgressGameState(initializedState, _winCheckers);
            }
        }
    }
}
