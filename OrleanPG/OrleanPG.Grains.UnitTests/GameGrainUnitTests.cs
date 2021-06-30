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
        public async Task StartAsync_OnNotInitialized_AssignsPlayers(AuthorizationToken tokenX, AuthorizationToken tokenO)
        {
            SetupAuthorizationTokens(tokenX, tokenO);

            await _game.StartAsync(tokenX, tokenO);

            _storeMock.Object.State.Should().Be(new GameState(tokenX, tokenO, GameStatus.XTurn, new GameMap()));
            _storeMock.Verify(x => x.WriteStateAsync(), Times.Once);
        }

        [Theory, AutoData]
        public async Task StartAsync_OnSuccess_NotifySubscribers(AuthorizationToken tokenX, AuthorizationToken tokenO, Guid grainId)
        {
            var streamMock = SetupStreamMock(grainId);
            SetupAuthorizationTokens(tokenX, tokenO);

            await _game.StartAsync(tokenX, tokenO);

            streamMock.Verify(x => x.OnNextAsync(new GameStatusDto(), null), Times.Once);
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
        public async Task StartAsync_OnInitialized_Throws(AuthorizationToken tokenX, AuthorizationToken tokenO)
        {
            SetupAuthorizationTokens(tokenX, tokenO);

            await _game.StartAsync(tokenX, tokenO);

            Func<Task> act = async () => await _game.StartAsync(tokenX, tokenO);
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Theory, AutoData]
        public async Task StartAsync_OnXPlayerNull_Throws(AuthorizationToken tokenO)
        {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            Func<Task> act = async () => await _game.StartAsync(null, tokenO);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
            await act.Should().ThrowAsync<ArgumentNullException>();
        }

        [Theory, AutoData]
        public async Task StartAsync_OnOPlayerNull_Throws(AuthorizationToken tokenX)
        {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            Func<Task> act = async () => await _game.StartAsync(tokenX, null);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
            await act.Should().ThrowAsync<ArgumentNullException>();
        }

        [Theory, AutoData]
        public async Task StartAsync_OnEqualPlayers_Throws(AuthorizationToken token)
        {
            Func<Task> act = async () => await _game.StartAsync(token, token);
            await act.Should().ThrowAsync<ArgumentException>();
        }

        [Theory, AutoData]
        public async Task StartAsync_OnNotInitialized_SetTimeoutReminder(AuthorizationToken playerX, AuthorizationToken playerO)
        {
            SetupAuthorizationTokens(playerX, playerO);

            await _game.StartAsync(playerX, playerO);

            _mockedGame.Verify(x => x.RegisterOrUpdateReminder(GameGrain.TimeoutCheckReminderName, GameGrain.TimeoutPeriod, GameGrain.TimeoutPeriod));
        }
        #endregion

        #region
        [Theory, AutoData]
        public async Task TurnAsync_OnNotInitialized_Throws(RandomValidXY position, AuthorizationToken token)
        {
            Func<Task> act = async () => await _game.TurnAsync(position.X, position.Y, token);
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        /// <summary>
        /// TODO: Test to big, split to three parts OR add state change notifier as separate entity
        /// </summary>
        [Theory, AutoData]
        public async Task TurnAsync_OnStateChangedByEngine_StoresChangesAndReturnNewStateAndNotifyObservers(
            GameState state, string xName, string oName, RandomValidXY position, Guid grainId)
        {
            _storeMock.Object.State = state;
            var streamMock = SetupStreamMock(grainId);
            SetupAuthorizationTokens(state, xName, oName);
            var updatedEngineState = state with { Status = state.Status.AnyExceptThis() };
            _gameEngineMock
                .Setup(g => g.Process(new UserTurnAction(position.X, position.Y, PlayerParticipation.X), state))
                .Returns(updatedEngineState);

            var result = await _game.TurnAsync(position.X, position.Y, state.XPlayer!);

            var expectedResult = new GameStatusDto(updatedEngineState.Status, updatedEngineState.Map, xName, oName);
            result.Should().BeEquivalentTo(expectedResult);
            _storeMock.Object.State.Should().Be(updatedEngineState);
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
                .Setup(g => g.Process(new UserTurnAction(0, 0, PlayerParticipation.X), state))
                .Returns(state);

            await _game.TurnAsync(0, 0, tokenX);

            _mockedGame.Verify(x => x.RegisterOrUpdateReminder(GameGrain.TimeoutCheckReminderName, GameGrain.TimeoutPeriod, GameGrain.TimeoutPeriod));
        }

        [Theory, AutoData]
        public async Task TurnAsync_OnYTurn_ResetsTimeoutReminder(AuthorizationToken tokenX, AuthorizationToken tokenO)
        {
            var state = _storeMock.Object.State with { XPlayer = tokenX, OPlayer = tokenO, Status = GameStatus.OTurn };
            _storeMock.Object.State = state;
            SetupAuthorizationTokens(tokenX, tokenO);
            _gameEngineMock
                .Setup(g => g.Process(new UserTurnAction(0, 0, PlayerParticipation.O), state))
                .Returns(state);

            await _game.TurnAsync(0, 0, tokenO);

            _mockedGame.Verify(x => x.RegisterOrUpdateReminder(GameGrain.TimeoutCheckReminderName, GameGrain.TimeoutPeriod, GameGrain.TimeoutPeriod));
        }
        #endregion

        /// <summary>
        /// TODO: Test is too big
        /// </summary>
        [Theory, AutoData]
        public async Task ReceiveReminder_OnStateChangedByEngine_StoresChangesAndNotifyObserversAndUnregisterReminder(
            GameState storageData, Guid grainId, string xName, string oName)
        {
            SetupAuthorizationTokens(storageData.XPlayer, storageData.OPlayer, xName, oName);
            var streamMock = SetupStreamMock(grainId);
            var reminderMock = new Mock<IGrainReminder>();
            _mockedGame.Setup(x => x.GetReminder(GameGrain.TimeoutCheckReminderName)).ReturnsAsync(reminderMock.Object);
            var _game = _mockedGame.Object;
            _storeMock.Object.State = storageData;
            var updatedEngineState = storageData with { Status = storageData.Status.AnyExceptThis() };
            _gameEngineMock.Setup(x => x.Process(TimeOutAction.Instance, storageData))
                .Returns(updatedEngineState);

            await _game.ReceiveReminder(GameGrain.TimeoutCheckReminderName, new TickStatus());

            _storeMock.Verify(x => x.WriteStateAsync(), Times.Once);
            _storeMock.Object.State.Should().Be(storageData with { Status = updatedEngineState.Status });
            var expectedResult = new GameStatusDto(updatedEngineState.Status, updatedEngineState.Map, xName, oName);
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
