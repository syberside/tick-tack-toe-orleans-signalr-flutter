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
            return ProcessMultipleDispatch(action as dynamic, engineState);
        }

        private IGameEngineState GetEngineState(GameState state)
        {
            if (state.XPlayer == null && state.OPlayer == null)
            {
                return new NotInitializedGameState();
            }
            else if (state.Status.IsEndStatus())
            {
                return new CompletedGameState(state);
            }
            else
            {
                return new InProgressGameState(state, _winCheckers);
            }
        }

        /// <summary>
        /// NOTE: Default callback for multuple dispatch
        /// </summary>
        private GameState ProcessMultipleDispatch(IGameAction action, IGameEngineState _)
            => throw new NotSupportedException($"Action {action?.GetType()} is not supported");


        private GameState ProcessMultipleDispatch(UserTurnAction action, IGameEngineState state)
            => state.Process(action);

        private GameState ProcessMultipleDispatch(TimeOutAction action, IGameEngineState state)
            => state.Process(action);
    }
}
