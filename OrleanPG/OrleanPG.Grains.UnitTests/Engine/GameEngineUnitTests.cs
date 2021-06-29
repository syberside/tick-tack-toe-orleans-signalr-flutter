using AutoFixture.Xunit2;
using FluentAssertions;
using Moq;
using OrleanPG.Grains.Game.Engine;
using OrleanPG.Grains.Game.Engine.Actions;
using OrleanPG.Grains.Game.Engine.WinCheckers;
using OrleanPG.Grains.Interfaces;
using OrleanPG.Grains.UnitTests.Helpers;
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
        public void Process_OnUnknownAction_Throws(GameEngineState engineState)
        {
            var arg = new ActionMock();
            Action action = () => _gameEngine.Process(arg, engineState);

            action.Should().Throw<NotSupportedException>();
        }

        private class ActionMock : IGameAction { }


        #region TimeOutAction
        [Theory]
        [InlineAutoData(GameState.TimedOut)]
        [InlineAutoData(GameState.Draw)]
        [InlineAutoData(GameState.XWin)]
        [InlineAutoData(GameState.OWin)]
        public void Process_TimeOutAction_OnFinalState_ReturnsUnchangedState(GameState state, GameEngineState engineState)
        {
            engineState = engineState with { GameState = state };

            var result = _gameEngine.Process(TimeOutAction.Instance, engineState);

            result.Should().BeEquivalentTo(engineState);
        }

        [Theory]
        [InlineAutoData(GameState.XTurn)]
        [InlineAutoData(GameState.OTurn)]
        public void Process_TimeOutAction_OnNotFinalState_EndsGame(GameState state, GameEngineState engineState)
        {
            engineState = engineState with { GameState = state };

            var result = _gameEngine.Process(TimeOutAction.Instance, engineState);
            var expected = engineState with { GameState = GameState.TimedOut };

            result.Should().BeEquivalentTo(expected);
        }
        #endregion

        //TODO: tests for UserTurnAction
        #region
        [Theory]
        [AutoData]
        public void Process_UserTurnAction_OnCellAlreadyInUse_Throws(int x, int y,
            PlayerParticipation participation, GameEngineState engineState)
        {
            (x, y) = GameMapTestHelper.AdjustToGameSize(x, y);
            engineState.Map[x, y] = CellStatus.X;

            Action action = () => _gameEngine.Process(new UserTurnAction(x, y, participation), engineState);

            action.Should().Throw<InvalidOperationException>();
        }

        [Theory]
        [InlineAutoData(PlayerParticipation.X, CellStatus.X, GameState.XTurn, GameState.OTurn)]
        [InlineAutoData(PlayerParticipation.O, CellStatus.O, GameState.OTurn, GameState.XTurn)]
        public void Process_UserTurnAction_OnFreeCell_ReturnsNextTurnState(
            PlayerParticipation participation, CellStatus cellStatus, GameState gameState,
            GameState expectedState,
            int x, int y, GameEngineState engineState)
        {
            (x, y) = GameMapTestHelper.AdjustToGameSize(x, y);
            engineState = engineState with { GameState = gameState };
            engineState.Map[x, y] = CellStatus.Empty;
            var action = new UserTurnAction(x, y, participation);

            var result = _gameEngine.Process(action, engineState);

            var expectedMap = engineState.Map.Clone();
            expectedMap[x, y] = cellStatus;
            var expectedResult = engineState with { GameState = expectedState, Map = expectedMap };
            result.Should().BeEquivalentTo(expectedResult);
        }


        [Theory]
        [InlineAutoData(CellStatus.X, GameState.XTurn, PlayerParticipation.X)]
        [InlineAutoData(CellStatus.O, GameState.OTurn, PlayerParticipation.O)]
        public void Process_UserTurnAction_OnLastCell_ReturnsDrawState(
            CellStatus cellStatus, GameState gameState, PlayerParticipation participation,
            int x, int y)
        {
            (x, y) = GameMapTestHelper.AdjustToGameSize(x, y);
            var map = GameMap.FilledWith(cellStatus);
            map[x, y] = CellStatus.Empty;
            var engineState = new GameEngineState(map, gameState);
            var action = new UserTurnAction(x, y, participation);

            var result = _gameEngine.Process(action, engineState);

            var expectedMap = GameMap.FilledWith(cellStatus);
            var expectedResult = new GameEngineState(expectedMap, GameState.Draw);
            result.Should().BeEquivalentTo(expectedResult);
        }
        #endregion
    }
}
