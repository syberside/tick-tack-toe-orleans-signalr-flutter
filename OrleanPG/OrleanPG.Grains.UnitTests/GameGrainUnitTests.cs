using AutoFixture.Xunit2;
using FluentAssertions;
using Moq;
using OrleanPG.Grains.Game;
using OrleanPG.Grains.Game.Engine;
using OrleanPG.Grains.Game.Engine.Actions;
using OrleanPG.Grains.GameLobbyGrain.UnitTests.Helpers;
using OrleanPG.Grains.Infrastructure;
using OrleanPG.Grains.Interfaces;
using Orleans;
using Orleans.Runtime;
using Orleans.Streams;
using System;
using System.Threading.Tasks;
using Xunit;

namespace OrleanPG.Grains.UnitTests
{
    public class GameGrainUnitTests
    {
        private readonly Mock<IPersistentState<GameState>> _storeMock;
        private readonly Mock<GameGrain> _mockedGame;
        private readonly Mock<IGrainIdProvider> _idProviderMock;
        private readonly Mock<IGameEngine> _gameEngineMock;
        private readonly GameGrain _game;


        public GameGrainUnitTests()
        {
            _storeMock = PersistanceHelper.CreateAndSetupStateWriteMock<GameState>();
            _idProviderMock = new();
            _gameEngineMock = new();
            _mockedGame = new(() => new GameGrain(_storeMock.Object, _idProviderMock.Object, _gameEngineMock.Object));
            // suppress base RegisterOrUpdateReminder calls
            _mockedGame.Setup(x => x.RegisterOrUpdateReminder(It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<TimeSpan>()))
                .ReturnsAsync(new Mock<IGrainReminder>().Object);
            _mockedGame.Setup(x => x.GetStreamProvider(It.IsAny<string>()))
                .Returns(new Mock<IStreamProvider>() { DefaultValue = DefaultValue.Mock }.Object);

            _game = _mockedGame.Object;
        }

        #region StartAsync
        [Theory, AutoData]
        public async Task StartAsync_IfStateChanged_NotifySubscribers(GameState state, string nameX, string nameO, Guid grainId)
        {
            var streamMock = SetupStreamMock(grainId);
            _storeMock.Object.State = state;
            SetupAuthorizationTokens(state.XPlayer, state.OPlayer, nameX, nameO);
            var updatedState = state with { Status = state.Status.AnyExceptThis() };
            _gameEngineMock.Setup(x => x.Process(It.IsAny<InitializeAction>(), It.IsAny<GameState>())).Returns(updatedState);

            await _game.StartAsync(state.XPlayer!, state.OPlayer!);

            var expectedUpdate = new GameStatusDto
            {
                GameMap = new GameMapDto(state.Map.DataSnapshot()),
                PlayerOName = nameO,
                PlayerXName = nameX,
                Status = updatedState.Status
            };
            streamMock.Verify(x => x.OnNextAsync(expectedUpdate, null), Times.Once);
        }

        private Mock<IAsyncStream<GameStatusDto>> SetupStreamMock(Guid grainId)
        {
            _idProviderMock.Setup(x => x.GetGrainId(_game)).Returns(grainId);
            var streamMock = new Mock<IAsyncStream<GameStatusDto>>();
            var providerMock = new Mock<IStreamProvider>();
            providerMock.Setup(x => x.GetStream<GameStatusDto>(grainId, Constants.GameUpdatesStreamName)).Returns(streamMock.Object);
            _mockedGame.Setup(x => x.GetStreamProvider(Constants.GameUpdatesStreamProviderName)).Returns(providerMock.Object);
            return streamMock;
        }

        [Theory, AutoData]
        public async Task StartAsync_Always_SetTimeoutReminder(GameState state, string nameX, string nameO)
        {
            _storeMock.Object.State = state;
            SetupAuthorizationTokens(state.XPlayer, state.OPlayer, nameX, nameO);
            _gameEngineMock.Setup(x => x.Process(It.IsAny<InitializeAction>(), It.IsAny<GameState>())).Returns(state);

            await _game.StartAsync(state.XPlayer!, state.OPlayer!);

            _mockedGame.Verify(x => x.RegisterOrUpdateReminder(GameGrain.TimeoutCheckReminderName, GameGrain.TimeoutPeriod, GameGrain.TimeoutPeriod));
        }
        #endregion

        #region
        /// <summary>
        /// TODO: Test to big, split to three parts OR add state change notifier as separate entity
        /// Each method calls engine, strores changes if updated state, notify subscribers => can be generalized
        /// </summary>
        [Theory, AutoData]
        public async Task TurnAsync_OnStateChangedByEngine_StoresChangesAndReturnNewStateAndNotifyObservers(
            GameState state, string xName, string oName, RandomizableMapPoint position, Guid grainId)
        {
            _storeMock.Object.State = state;
            var streamMock = SetupStreamMock(grainId);
            SetupAuthorizationTokens(state, xName, oName);
            var updatedState = state with { Status = state.Status.AnyExceptThis() };
            _gameEngineMock
                .Setup(g => g.Process(new UserTurnAction(position, PlayerParticipation.X), state))
                .Returns(updatedState);

            var result = await _game.TurnAsync(position, state.XPlayer!);

            var gameMapDto = new GameMapDto(updatedState.Map.DataSnapshot());
            var expectedResult = new GameStatusDto(updatedState.Status, gameMapDto, xName, oName);
            result.Should().BeEquivalentTo(expectedResult);
            _storeMock.Object.State.Should().Be(updatedState);
            _storeMock.Verify(x => x.WriteStateAsync(), Times.Once);
            streamMock.Verify(x => x.OnNextAsync(expectedResult, null), Times.Once);
        }

        [Theory, AutoData]
        public async Task TurnAsync_OnXTurn_ResetsTimeoutReminder(
            AuthorizationToken tokenX, AuthorizationToken tokenO)
        {
            var state = _storeMock.Object.State with { XPlayer = tokenX, OPlayer = tokenO, Status = GameStatus.OTurn };
            _storeMock.Object.State = state;
            SetupAuthorizationTokens(tokenX, tokenO);
            _gameEngineMock
                .Setup(g => g.Process(new UserTurnAction(GameMapPoint.Origin, PlayerParticipation.X), state))
                .Returns(state);

            await _game.TurnAsync(GameMapPoint.Origin, tokenX);

            _mockedGame.Verify(x => x.RegisterOrUpdateReminder(GameGrain.TimeoutCheckReminderName, GameGrain.TimeoutPeriod, GameGrain.TimeoutPeriod));
        }

        [Theory, AutoData]
        public async Task TurnAsync_OnYTurn_ResetsTimeoutReminder(AuthorizationToken tokenX, AuthorizationToken tokenO)
        {
            var state = _storeMock.Object.State with { XPlayer = tokenX, OPlayer = tokenO, Status = GameStatus.OTurn };
            _storeMock.Object.State = state;
            SetupAuthorizationTokens(tokenX, tokenO);
            _gameEngineMock
                .Setup(g => g.Process(new UserTurnAction(GameMapPoint.Origin, PlayerParticipation.O), state))
                .Returns(state);

            await _game.TurnAsync(GameMapPoint.Origin, tokenO);

            _mockedGame.Verify(x => x.RegisterOrUpdateReminder(GameGrain.TimeoutCheckReminderName, GameGrain.TimeoutPeriod, GameGrain.TimeoutPeriod));
        }
        #endregion

        /// <summary>
        /// TODO: Test is too big
        /// </summary>
        [Theory, AutoData]
        public async Task ReceiveReminder_OnStateChangedByEngine_StoresChangesAndNotifyObserversAndUnregisterReminder(
            GameState state, Guid grainId, string xName, string oName)
        {
            SetupAuthorizationTokens(state.XPlayer, state.OPlayer, xName, oName);
            var streamMock = SetupStreamMock(grainId);
            var reminderMock = new Mock<IGrainReminder>();
            _mockedGame.Setup(x => x.GetReminder(GameGrain.TimeoutCheckReminderName)).ReturnsAsync(reminderMock.Object);
            var _game = _mockedGame.Object;
            _storeMock.Object.State = state;
            var updatedState = state with { Status = state.Status.AnyExceptThis() };
            _gameEngineMock.Setup(x => x.Process(TimeOutAction.Instance, state))
                .Returns(updatedState);

            await _game.ReceiveReminder(GameGrain.TimeoutCheckReminderName, new TickStatus());

            _storeMock.Verify(x => x.WriteStateAsync(), Times.Once);
            _storeMock.Object.State.Should().Be(state with { Status = updatedState.Status });
            var gameMapDto = new GameMapDto(updatedState.Map.DataSnapshot());
            var expectedResult = new GameStatusDto(updatedState.Status, gameMapDto, xName, oName);
            streamMock.Verify(x => x.OnNextAsync(expectedResult, null), Times.Once);
            _mockedGame.Verify(x => x.UnregisterReminder(reminderMock.Object), Times.Once);
        }

        #region Helpers
        private void SetupAuthorizationTokens(AuthorizationToken?[] tokens, string?[] userNames)
        {
            var grainFactoryMock = new Mock<IGrainFactory>();
            _mockedGame.Setup(x => x.GrainFactory).Returns(grainFactoryMock.Object);
            var lobbyMock = new Mock<IGameLobby>();
            lobbyMock.Setup(x => x.ResolveUserNamesAsync(tokens)).ReturnsAsync(userNames);
            grainFactoryMock.Setup(x => x.GetGrain<IGameLobby>(Guid.Empty, It.IsAny<string>())).Returns(lobbyMock.Object);
        }

        private void SetupAuthorizationTokens(GameState data, string? nameX = null, string? nameO = null)
            => SetupAuthorizationTokens(new AuthorizationToken?[] { data.XPlayer, data.OPlayer }, new string?[] { nameX, nameO });

        private void SetupAuthorizationTokens(AuthorizationToken? tokenX = null, AuthorizationToken? tokenO = null, string? nameX = null, string? nameO = null)
            => SetupAuthorizationTokens(new AuthorizationToken?[] { tokenX, tokenO }, new string?[] { nameX, nameO });

        #endregion
    }
}
