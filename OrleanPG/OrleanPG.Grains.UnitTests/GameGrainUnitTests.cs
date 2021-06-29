using AutoFixture.Xunit2;
using FluentAssertions;
using Moq;
using OrleanPG.Grains.Game;
using OrleanPG.Grains.Game.Engine;
using OrleanPG.Grains.Game.Engine.Actions;
using OrleanPG.Grains.GameLobbyGrain.UnitTests.Helpers;
using OrleanPG.Grains.Infrastructure;
using OrleanPG.Grains.Interfaces;
using OrleanPG.Grains.UnitTests.Helpers;
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
        private readonly Mock<IPersistentState<GameStorageData>> _storeMock;
        private readonly Mock<GameGrain> _mockedGame;
        private readonly Mock<IGrainIdProvider> _idProviderMock;
        private readonly Mock<IGameEngine> _gameEngineMock;
        private readonly GameGrain _game;


        public GameGrainUnitTests()
        {
            _storeMock = PersistanceHelper.CreateAndSetupStateWriteMock<GameStorageData>();
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

            _storeMock.Object.State.Should().Be(new GameStorageData(tokenX, tokenO, GameState.XTurn, new GameMap()));
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
        public async Task TurnAsync_OnNotInitialized_Throws(int x, int y, AuthorizationToken token)
        {
            Func<Task> act = async () => await _game.TurnAsync(x, y, token);
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        /// <summary>
        /// TODO: Test to big, split to three parts OR add state change notifier as separate entity
        /// </summary>
        [Theory, AutoData]
        public async Task TurnAsync_OnStateChangedByEngine_StoresChangesAndReturnNewStateAndNotifyObservers(
            AuthorizationToken tokenX, AuthorizationToken tokenO, string xName, string oName,
            int x, int y, GameEngineState engineState, Guid grainId)
        {
            var streamMock = SetupStreamMock(grainId);
            (x, y) = GameMapTestHelper.AdjustToGameSize(x, y);
            SetupStoreMockByEngineState(tokenX, tokenO, engineState);
            SetupAuthorizationTokens(tokenX, tokenO, xName, oName);
            var updatedEngineState = engineState with { GameState = engineState.GameState.AnyExceptThis() };
            _gameEngineMock
                .Setup(g => g.Process(new UserTurnAction(x, y, PlayerParticipation.X), engineState))
                .Returns(updatedEngineState);

            var result = await _game.TurnAsync(x, y, tokenX);

            var expectedResult = new GameStatusDto(updatedEngineState.GameState, updatedEngineState.Map, xName, oName);
            result.Should().BeEquivalentTo(expectedResult);
            var expectedStorageState = new GameStorageData(tokenX, tokenO, updatedEngineState.GameState, updatedEngineState.Map);
            _storeMock.Object.State.Should().Be(expectedStorageState);
            _storeMock.Verify(x => x.WriteStateAsync(), Times.Once);
            streamMock.Verify(x => x.OnNextAsync(expectedResult, null), Times.Once);
        }

        private void SetupStoreMockByEngineState(AuthorizationToken tokenX, AuthorizationToken tokenO, GameEngineState engineState)
        {
            _storeMock.Object.State = _storeMock.Object.State with
            {
                XPlayer = tokenX,
                OPlayer = tokenO,
                Map = engineState.Map,
                Status = engineState.GameState,
            };
        }

        [Theory, AutoData]
        public async Task TurnAsync_OnXTurn_ResetsTimeoutReminder(
            AuthorizationToken tokenX, AuthorizationToken tokenO)
        {
            _storeMock.Object.State = _storeMock.Object.State with { XPlayer = tokenX, OPlayer = tokenO, Status = GameState.OTurn };
            SetupAuthorizationTokens(tokenX, tokenO);
            var engineState = new GameEngineState(_storeMock.Object.State.Map, _storeMock.Object.State.Status);
            _gameEngineMock
                .Setup(g => g.Process(new UserTurnAction(0, 0, PlayerParticipation.X), engineState))
                .Returns(engineState);

            await _game.TurnAsync(0, 0, tokenX);

            _mockedGame.Verify(x => x.RegisterOrUpdateReminder(GameGrain.TimeoutCheckReminderName, GameGrain.TimeoutPeriod, GameGrain.TimeoutPeriod));
        }

        [Theory, AutoData]
        public async Task TurnAsync_OnYTurn_ResetsTimeoutReminder(
            AuthorizationToken tokenX, AuthorizationToken tokenO)
        {
            _storeMock.Object.State = _storeMock.Object.State with { XPlayer = tokenX, OPlayer = tokenO, Status = GameState.OTurn };
            SetupAuthorizationTokens(tokenX, tokenO);
            var engineState = new GameEngineState(_storeMock.Object.State.Map, _storeMock.Object.State.Status);
            _gameEngineMock
                .Setup(g => g.Process(new UserTurnAction(0, 0, PlayerParticipation.O), engineState))
                .Returns(engineState);

            await _game.TurnAsync(0, 0, tokenO);

            _mockedGame.Verify(x => x.RegisterOrUpdateReminder(GameGrain.TimeoutCheckReminderName, GameGrain.TimeoutPeriod, GameGrain.TimeoutPeriod));
        }
        #endregion

        /// <summary>
        /// TODO: Test is too big
        /// </summary>
        [Theory, AutoData]
        public async Task ReceiveReminder_OnStateChangedByEngine_StoresChangesAndNotifyObserversAndUnregisterReminder(
            GameStorageData storageData, Guid grainId, string xName, string oName)
        {
            SetupAuthorizationTokens(storageData.XPlayer, storageData.OPlayer, xName, oName);
            var streamMock = SetupStreamMock(grainId);
            var reminderMock = new Mock<IGrainReminder>();
            _mockedGame.Setup(x => x.GetReminder(GameGrain.TimeoutCheckReminderName)).ReturnsAsync(reminderMock.Object);
            var _game = _mockedGame.Object;
            _storeMock.Object.State = storageData;
            var gameEngineState = new GameEngineState(storageData.Map, storageData.Status);
            var updatedEngineState = gameEngineState with { GameState = gameEngineState.GameState.AnyExceptThis() };
            _gameEngineMock.Setup(x => x.Process(TimeOutAction.Instance, gameEngineState))
                .Returns(updatedEngineState);

            await _game.ReceiveReminder(GameGrain.TimeoutCheckReminderName, new TickStatus());

            _storeMock.Verify(x => x.WriteStateAsync(), Times.Once);
            _storeMock.Object.State.Should().Be(storageData with { Status = updatedEngineState.GameState });
            var expectedResult = new GameStatusDto(updatedEngineState.GameState, updatedEngineState.Map, xName, oName);
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
        private void SetupAuthorizationTokens(AuthorizationToken? tokenX = null, AuthorizationToken? tokenO = null, string? nameX = null, string? nameO = null)
            => SetupAuthorizationTokens(new AuthorizationToken?[] { tokenX, tokenO }, new string?[] { nameX, nameO });

        #endregion
    }
}
