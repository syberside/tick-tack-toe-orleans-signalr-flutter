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

        #region
        [Theory]
        [AutoData]
        public void Process_UserTurnAction_OnCellAlreadyInUse_Throws(RandomizableUserTurnAction action, GameState state)
        {
            state = state with { Map = state.Map.Update(action.Position, CellStatus.X) };

            Action act = () => _gameEngine.Process(action, state);

            act.Should().Throw<InvalidOperationException>();
        }

        [Theory]
        [InlineAutoData(PlayerParticipation.X, CellStatus.X, GameStatus.XTurn, GameStatus.OTurn)]
        [InlineAutoData(PlayerParticipation.O, CellStatus.O, GameStatus.OTurn, GameStatus.XTurn)]
        public void Process_UserTurnAction_OnFreeCell_ReturnsNextTurnState(
            PlayerParticipation participation, CellStatus cellStatus, GameStatus status,
            GameStatus expectedState, RandomizableMapPoint position, GameState state)
        {
            state = state with
            {
                Status = status,
                Map = state.Map.Update(position, CellStatus.Empty)
            };
            var action = new UserTurnAction(position, participation);

            var result = _gameEngine.Process(action, state);

            var expectedMap = state.Map.Update(action.Position, cellStatus);
            var expectedResult = state with { Status = expectedState, Map = expectedMap };
            result.Should().BeEquivalentTo(expectedResult);
        }


        [Theory]
        [InlineAutoData(CellStatus.X, GameStatus.XTurn, PlayerParticipation.X)]
        [InlineAutoData(CellStatus.O, GameStatus.OTurn, PlayerParticipation.O)]
        public void Process_UserTurnAction_OnLastCell_ReturnsDrawState(
            CellStatus cellStatus, GameStatus status, PlayerParticipation participation,
            GameState state, RandomizableMapPoint position)
        {
            var map = GameMap.FilledWith(cellStatus).Update(position, CellStatus.Empty);
            state = state with { Status = status, Map = map };
            var action = new UserTurnAction(position, participation);

            var result = _gameEngine.Process(action, state);

            var expectedMap = GameMap.FilledWith(cellStatus);
            var expectedResult = state with { Map = expectedMap, Status = GameStatus.Draw };
            result.Should().BeEquivalentTo(expectedResult);
        }


        [Theory, AutoData]
        public void Process_UserTurnAction_OnNotInitializedByX_Throws(RandomizableUserTurnAction action, GameState state)
        {
            state = state with { XPlayer = null };

            Action act = () => _gameEngine.Process(action, state);

            act.Should().Throw<ArgumentException>();
        }

        [Theory, AutoData]
        public void Process_UserTurnAction_OnNotInitializedByO_Throws(RandomizableUserTurnAction action, GameState state)
        {
            state = state with { OPlayer = null };

            Action act = () => _gameEngine.Process(action, state);

            act.Should().Throw<ArgumentException>();
        }
        #endregion

        #region InitializeAction
        [Theory, AutoData]
        public void Process_InitializeAction_OnNotInitialized_AssignsPlayers(InitializeAction action, GameState state)
        {
            state = state with { OPlayer = null, XPlayer = null };

            var result = _gameEngine.Process(action, state);

            var expected = state with { XPlayer = action.XPlayer, OPlayer = action.OPlayer };
            result.Should().BeEquivalentTo(expected);
        }

        [Theory, AutoData]
        public void Process_InitializeAction_OnInitialized_Throws(InitializeAction action, GameState state)
        {
            Action act = () => _gameEngine.Process(action, state);
            act.Should().Throw<InvalidOperationException>();
        }

        #endregion
    }

    public record RandomizableUserTurnAction : UserTurnAction
    {
        public RandomizableUserTurnAction(RandomizableMapPoint position, PlayerParticipation participation)
            : base(position, participation)
        {
        }
    }
}
