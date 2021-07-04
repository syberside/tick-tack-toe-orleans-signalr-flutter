using OrleanPG.Grains.Game.Engine.Actions;
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
                return new NotInitializedGameEngineState();
            }
            if (state.Status.IsEndStatus())
            {
                return new CompletedGameEngineState(state);
            }

            return new GameInProgressGameEngineState(state, _winCheckers);
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


        private static GameStatus StepToNewStep(PlayerParticipation p)
            => p == PlayerParticipation.X ? GameStatus.OTurn : GameStatus.XTurn;

        private static GameStatus StepToWinState(PlayerParticipation p)
            => p == PlayerParticipation.X ? GameStatus.XWin : GameStatus.OWin;

        private interface IGameEngineState
        {
            GameState Process(UserTurnAction action);
            GameState Process(TimeOutAction action);
        }


        private class NotInitializedGameEngineState : IGameEngineState
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

        private class GameInProgressGameEngineState : IGameEngineState
        {
            private readonly GameState _state;
            private readonly IWinChecker[] _winCheckers;

            public GameInProgressGameEngineState(GameState gameState, IWinChecker[] winCheckers)
            {
                _state = gameState;
                _winCheckers = winCheckers;
            }

            public GameState Process(UserTurnAction action)
            {
                if (!_state.IsInitialized)
                {
                    throw new InvalidOperationException("Game is not initialized yet");
                }
                var map = _state.Map;
                if (map.IsCellBusy(action.Position))
                {
                    throw new InvalidOperationException($"Cell {action.Position} already allocated by {(map[action.Position] == CellStatus.X ? "X" : "O")}");
                }

                var gameState = _state.Status;
                if (gameState.IsEndStatus())
                {
                    throw new InvalidOperationException($"Game is in end status: {gameState}");
                }
                var (expectedNextPlayer, stepMarker) = PlayerParticiptionExtensions.PlayerForState(gameState);
                if (expectedNextPlayer != action.StepBy)
                {
                    throw new InvalidOperationException();
                }

                var updatedMap = map.Update(action.Position, stepMarker);
                var status = GetNewStatus(action.StepBy, updatedMap);

                return _state with { Status = status, Map = updatedMap };
            }

            private GameStatus GetNewStatus(PlayerParticipation stepBy, GameMap map)
            {
                if (!map.HaveEmptyCells)
                {
                    return GameStatus.Draw;
                }
                var win = _winCheckers
                  .Select(x => x.CheckIfWin(map, stepBy))
                  .Where(x => x != null)
                  .FirstOrDefault();
                // NOTE: Win content is currently not used, but will be used for drawing a game result in UI in the future
                if (win == null)
                {
                    return StepToNewStep(stepBy);
                }
                else
                {
                    return StepToWinState(stepBy);
                }
            }

            public GameState Process(TimeOutAction action)
            {
                if (_state.Status.IsEndStatus())
                {
                    return _state;
                }
                return _state with { Status = GameStatus.TimedOut };
            }
        }

        private class CompletedGameEngineState : IGameEngineState
        {
            private readonly GameState _state;

            public CompletedGameEngineState(GameState state) => _state = state;

            public GameState Process(UserTurnAction action)
            {
                throw new InvalidOperationException();
            }

            public GameState Process(TimeOutAction action) => _state;
        }
    }
}
