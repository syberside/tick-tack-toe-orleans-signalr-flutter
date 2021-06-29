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

        public GameEngineState Process<TAction>(TAction action, GameEngineState state) where TAction : IGameAction
        {
            return Process(action as dynamic, state);
        }

        /// <summary>
        /// NOTE: Default callback for multuple dispatch
        /// </summary>
        private GameEngineState Process(IGameAction action, GameEngineState _)
            => throw new NotSupportedException($"Action {action?.GetType()} is not supported");


        private GameEngineState Process(UserTurnAction action, GameEngineState state)
        {
            var x = action.X;
            var y = action.Y;
            var map = state.Map;
            if (map.IsCellBusy(x, y))
            {
                throw new InvalidOperationException($"Cell {{{x};{y}}} already allocated by {(map[x, y] == CellStatus.X ? "X" : "O")}");
            }

            var gameState = state.GameState;
            if (gameState.IsEndStatus())
            {
                throw new InvalidOperationException($"Game is in end status: {gameState}");
            }
            var (expectedNextPlayer, stepMarker) = PlayerParticiptionExtensions.PlayerForState(gameState);
            if (expectedNextPlayer != action.StepBy)
            {
                throw new InvalidOperationException();
            }

            var updatedMap = UpdateMap(x, y, map, stepMarker);
            var status = GetNewStatus(action.StepBy, updatedMap);

            return new GameEngineState(updatedMap, status);
        }

        private GameEngineState Process(TimeOutAction _, GameEngineState engineState)
        {
            if (engineState.GameState.IsEndStatus())
            {
                return engineState;
            }
            return engineState with { GameState = GameState.TimedOut };
        }

        private static GameMap UpdateMap(int x, int y, GameMap map, CellStatus stepMarker)
        {
            var updatedMap = map.Clone();
            updatedMap[x, y] = stepMarker;
            return updatedMap;
        }

        private GameState GetNewStatus(PlayerParticipation stepBy, GameMap map)
        {
            if (!map.HaveEmptyCells)
            {
                return GameState.Draw;
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
        private static GameState StepToNewStep(PlayerParticipation p)
            => p == PlayerParticipation.X ? GameState.OTurn : GameState.XTurn;

        private static GameState StepToWinState(PlayerParticipation p)
            => p == PlayerParticipation.X ? GameState.XWin : GameState.OWin;
    }
}
