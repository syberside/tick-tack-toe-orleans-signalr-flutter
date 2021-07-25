using OrleanPG.Grains.Game.Engine.Actions;
using OrleanPG.Grains.Game.Engine.WinCheckers;
using OrleanPG.Grains.Interfaces;
using System;
using System.Linq;

namespace OrleanPG.Grains.Game.Engine.GameEngineStates
{
    internal class InProgressGameState : IGameEngineState
    {
        private readonly InitializedGameState _state;
        private readonly IWinChecker[] _winCheckers;

        public InProgressGameState(InitializedGameState gameState, IWinChecker[] winCheckers)
        {
            _state = gameState;
            _winCheckers = winCheckers;
        }

        public GameState Process(UserTurnAction action)
        {
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

            return _state.AsGameState() with { Status = status, Map = updatedMap };
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
                return _state.AsGameState();
            }
            return _state.AsGameState() with { Status = GameStatus.TimedOut };
        }

        private static GameStatus StepToNewStep(PlayerParticipation p)
       => p == PlayerParticipation.X ? GameStatus.OTurn : GameStatus.XTurn;

        private static GameStatus StepToWinState(PlayerParticipation p)
            => p == PlayerParticipation.X ? GameStatus.XWin : GameStatus.OWin;

        public GameState Process(InitializeAction action) => throw new InvalidOperationException();

        internal class InitializedGameState
        {
            private readonly GameState _state;
            public GameState AsGameState() => _state;

            public GameMap Map => _state.Map;

            public GameStatus Status => _state.Status;

            public InitializedGameState(GameState state)
            {
                if (!state.IsInitialized)
                {
                    throw new ArgumentException($"State is not initialized: {state}");
                }
                _state = state;
            }
        }
    }
}
