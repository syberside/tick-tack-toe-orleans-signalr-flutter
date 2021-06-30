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
            return ProcessMultipleDispatch(action as dynamic, state);
        }

        /// <summary>
        /// NOTE: Default callback for multuple dispatch
        /// </summary>
        private GameState ProcessMultipleDispatch(IGameAction action, GameState _)
            => throw new NotSupportedException($"Action {action?.GetType()} is not supported");


        private GameState ProcessMultipleDispatch(UserTurnAction action, GameState state)
        {
            if (!state.IsInitialized)
            {
                throw new InvalidOperationException("Game is not initialized yet");
            }
            var map = state.Map;
            if (map.IsCellBusy(action.Position))
            {
                throw new InvalidOperationException($"Cell {action.Position} already allocated by {(map[action.Position] == CellStatus.X ? "X" : "O")}");
            }

            var gameState = state.Status;
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

            return state with { Status = status, Map = updatedMap };
        }

        private GameState ProcessMultipleDispatch(TimeOutAction _, GameState state)
        {
            if (state.Status.IsEndStatus())
            {
                return state;
            }
            return state with { Status = GameStatus.TimedOut };
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
        private static GameStatus StepToNewStep(PlayerParticipation p)
            => p == PlayerParticipation.X ? GameStatus.OTurn : GameStatus.XTurn;

        private static GameStatus StepToWinState(PlayerParticipation p)
            => p == PlayerParticipation.X ? GameStatus.XWin : GameStatus.OWin;
    }
}
