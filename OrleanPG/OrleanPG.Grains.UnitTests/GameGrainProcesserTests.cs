using AutoFixture.Xunit2;
using Moq;
using OrleanPG.Grains.Game;
using OrleanPG.Grains.Game.Engine;
using OrleanPG.Grains.Game.Engine.Actions;
using OrleanPG.Grains.Game.GrainLogic;
using OrleanPG.Grains.Interfaces;
using OrleanPG.Grains.UnitTests.Helpers;
using Orleans.Streams;
using System.Threading.Tasks;
using Xunit;

namespace OrleanPG.Grains.UnitTests
{

    public class GameGrainProcesserTests
    {
        private readonly GameGrainLogic _processor;
        private readonly Mock<IGameStateStore> _gameStateStore = new();
        private readonly Mock<IGameEngine> _gameEngine = new();
        private readonly Mock<IGameLobby> _gameLobby = new();
        private readonly Mock<IAsyncStream<GameStatusDto>> _updatesStream = new();
        private readonly Mock<IReminderHandle> _reminderHandle = new();

        public GameGrainProcesserTests()
        {
            _processor = new GameGrainLogic(_gameStateStore.Object,
                                                       _gameEngine.Object,
                                                       _gameLobby.Object,
                                                       _updatesStream.Object,
                                                       _reminderHandle.Object);
        }

        [Theory, AutoData]
        public async Task ProcessAction_OnStateUpdate_WritesStateAndNotifyObservers(
            TestAction action, GameState state, string xName, string oName)
        {
            _gameStateStore.Setup(x => x.GetState()).Returns(state);
            _gameLobby
                .Setup(x => x.ResolveUserNamesAsync(new[] { state.XPlayer, state.OPlayer }))
                .ReturnsAsync(new[] { xName, oName });
            var changedState = state with { Status = state.Status.AnyExceptThis() };
            _gameEngine.Setup(x => x.Process(action, state)).Returns(changedState);
            await _processor.ProcessAction(action);

            _gameStateStore.Verify(x => x.WriteState(changedState), Times.Once);
            var gameMapDto = new GameMapDto(changedState.Map.DataSnapshot());
            var expectedUpdate = new GameStatusDto(changedState.Status, gameMapDto, xName, oName);
            _updatesStream.Verify(x => x.OnNextAsync(expectedUpdate, null), Times.Once);
        }

        [Theory, AutoData]
        public async Task ProcessAction_OnNoStateUpdates_DoesntNotifyObservers(TestAction action, GameState state)
        {
            _gameStateStore.Setup(x => x.GetState()).Returns(state);
            _gameEngine.Setup(x => x.Process(action, state)).Returns(state);

            await _processor.ProcessAction(action);

            _updatesStream.Verify(x => x.OnNextAsync(It.IsAny<GameStatusDto>(), null), Times.Never);
        }

        [Theory, AutoData]
        public async Task ProcessAction_OnFinalStatus_ResetTimer(TestAction action, GameState state)
        {
            state = state with { Status = state.Status.AnyFinalExpectThis() };
            _gameStateStore.Setup(x => x.GetState()).Returns(state);
            _gameEngine.Setup(x => x.Process(action, state)).Returns(state);

            await _processor.ProcessAction(action);

            _reminderHandle.Verify(x => x.Reset(), Times.Once);
        }

        [Theory, AutoData]
        public async Task ProcessAction_OnProgressStatus_SetTimer(TestAction action, GameState state)
        {
            state = state with { Status = state.Status.AnyNotFinalExpectThis() };
            _gameStateStore.Setup(x => x.GetState()).Returns(state);
            _gameEngine.Setup(x => x.Process(action, state)).Returns(state);

            await _processor.ProcessAction(action);

            _reminderHandle.Verify(x => x.Set(), Times.Once);
        }

        public class TestAction : IGameAction { }
    }
}
