using AutoFixture.Xunit2;
using FluentAssertions;
using Moq;
using OrleanPG.Grains.Game;
using OrleanPG.Grains.Game.Engine;
using OrleanPG.Grains.Game.Engine.Actions;
using OrleanPG.Grains.Game.Engine.WinCheckers;
using OrleanPG.Grains.Interfaces;
using System;
using Xunit;

namespace OrleanPG.Grains.UnitTests.Engine
{
    public class GameEngineUnitTests
    {
        private readonly GameEngine _gameEngine;
        private readonly Mock<IWinChecker> _firstWinChecker = new();
        private readonly Mock<IWinChecker> _secondWinChecker = new();


        public GameEngineUnitTests()
        {
            _gameEngine = new GameEngine(new[] { _firstWinChecker.Object, _secondWinChecker.Object });
        }

        [Theory]
        [AutoData]
        public void Process_OnUnknownAction_Throws(GameState state)
        {
            var arg = new ActionMock();
            Action action = () => _gameEngine.Process(arg, state);

            action.Should().Throw<NotSupportedException>();
        }

        private class ActionMock : IGameAction { }


        #region TimeOutAction
        [Theory]
        [InlineAutoData(GameStatus.TimedOut)]
        [InlineAutoData(GameStatus.Draw)]
        [InlineAutoData(GameStatus.XWin)]
        [InlineAutoData(GameStatus.OWin)]
        public void Process_TimeOutAction_OnFinalState_ReturnsUnchangedState(GameStatus status, GameState state)
        {
            state = state with { Status = status };

            var result = _gameEngine.Process(TimeOutAction.Instance, state);

            result.Should().BeEquivalentTo(state);
        }

        [Theory]
        [InlineAutoData(GameStatus.XTurn)]
        [InlineAutoData(GameStatus.OTurn)]
        public void Process_TimeOutAction_OnNotFinalState_EndsGame(GameStatus status, GameState state)
        {
            state = state with { Status = status };

            var result = _gameEngine.Process(TimeOutAction.Instance, state);
            var expected = state with { Status = GameStatus.TimedOut };

            result.Should().BeEquivalentTo(expected);
        }
        #endregion

        //TODO: tests for UserTurnAction
        #region
        [Theory]
        [AutoData]
        public void Process_UserTurnAction_OnCellAlreadyInUse_Throws(
            RandomizableMapPoint position, PlayerParticipation participation, GameState state)
        {
            state.Map[position.X, position.Y] = CellStatus.X;

            Action action = () => _gameEngine.Process(new UserTurnAction(position, participation), state);

            action.Should().Throw<InvalidOperationException>();
        }

        [Theory]
        [InlineAutoData(PlayerParticipation.X, CellStatus.X, GameStatus.XTurn, GameStatus.OTurn)]
        [InlineAutoData(PlayerParticipation.O, CellStatus.O, GameStatus.OTurn, GameStatus.XTurn)]
        public void Process_UserTurnAction_OnFreeCell_ReturnsNextTurnState(
            PlayerParticipation participation, CellStatus cellStatus, GameStatus status,
            GameStatus expectedState,
            RandomizableMapPoint position, GameState state)
        {
            state = state with { Status = status };
            state.Map[position.X, position.Y] = CellStatus.Empty;
            var action = new UserTurnAction(position, participation);

            var result = _gameEngine.Process(action, state);

            var expectedMap = state.Map.Clone();
            expectedMap[position.X, position.Y] = cellStatus;
            var expectedResult = state with { Status = expectedState, Map = expectedMap };
            result.Should().BeEquivalentTo(expectedResult);
        }


        [Theory]
        [InlineAutoData(CellStatus.X, GameStatus.XTurn, PlayerParticipation.X)]
        [InlineAutoData(CellStatus.O, GameStatus.OTurn, PlayerParticipation.O)]
        public void Process_UserTurnAction_OnLastCell_ReturnsDrawState(
            CellStatus cellStatus, GameStatus status, PlayerParticipation participation,
            RandomizableMapPoint position)
        {
            var map = GameMap.FilledWith(cellStatus);
            map[position.X, position.Y] = CellStatus.Empty;
            var state = new GameState(null, null, status, map);
            var action = new UserTurnAction(position, participation);

            var result = _gameEngine.Process(action, state);

            var expectedMap = GameMap.FilledWith(cellStatus);
            var expectedResult = new GameState(null, null, GameStatus.Draw, expectedMap);
            result.Should().BeEquivalentTo(expectedResult);
        }
        #endregion
    }
}
