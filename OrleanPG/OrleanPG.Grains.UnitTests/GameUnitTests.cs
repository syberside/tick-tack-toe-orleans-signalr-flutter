using AutoFixture.Xunit2;
using FluentAssertions;
using Moq;
using OrleanPG.Grains.GameGrain;
using OrleanPG.Grains.GameLobbyGrain.UnitTests.Helpers;
using OrleanPG.Grains.Infrastructure;
using OrleanPG.Grains.Interfaces;
using Orleans.Runtime;
using Orleans.Streams;
using System;
using System.Threading.Tasks;
using Xunit;

namespace OrleanPG.Grains.UnitTests
{
    public class GameUnitTests
    {
        private readonly Mock<IPersistentState<GameStorageData>> _storeMock;
        private readonly Mock<Game> _mockedGame;
        private readonly Mock<IGrainIdProvider> _idProviderMock;
        private readonly Game _game;


        public GameUnitTests()
        {
            _storeMock = PersistanceHelper.CreateAndSetupStateWriteMock<GameStorageData>();
            _idProviderMock = new();
            _mockedGame = new Mock<Game>(() => new Game(_storeMock.Object, _idProviderMock.Object));
            // suppress base RegisterOrUpdateReminder calls
            _mockedGame.Setup(x => x.RegisterOrUpdateReminder(It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<TimeSpan>())).ReturnsAsync((IGrainReminder)null);
            // TODO: Check
            _mockedGame.Setup(x => x.GetStreamProvider(It.IsAny<string>())).Returns(new Mock<IStreamProvider>() { DefaultValue = DefaultValue.Mock }.Object);
            _game = _mockedGame.Object;
        }

        #region StartAsync
        [Theory, AutoData]
        public async Task StartAsync_OnNotInitialized_AssignsPlayers(AuthorizationToken tokenX, AuthorizationToken tokenO)
        {
            await _game.StartAsync(tokenX, tokenO);

            _storeMock.Object.State.Should().Be(new GameStorageData(tokenX, tokenO, GameState.XTurn, new GameMap()));
            _storeMock.Verify(x => x.WriteStateAsync(), Times.Once);
        }

        [Theory, AutoData]
        public async Task StartAsync_OnSuccess_NotifySubscribers(AuthorizationToken tokenX, AuthorizationToken tokenO, Guid grainId)
        {
            var streamMock = SetupStreamMock(grainId);

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
            await _game.StartAsync(tokenX, tokenO);

            Func<Task> act = async () => await _game.StartAsync(tokenX, tokenO);
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Theory, AutoData]
        public async Task StartAsync_OnXPlayerNull_Throws(AuthorizationToken tokenO)
        {
            Func<Task> act = async () => await _game.StartAsync(null, tokenO);
            await act.Should().ThrowAsync<ArgumentNullException>();
        }

        [Theory, AutoData]
        public async Task StartAsync_OnOPlayerNull_Throws(AuthorizationToken tokenX)
        {
            Func<Task> act = async () => await _game.StartAsync(tokenX, null);
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
            await _game.StartAsync(playerX, playerO);

            _mockedGame.Verify(x => x.RegisterOrUpdateReminder(Game.TimeoutCheckReminderName, Game.TimeoutPeriod, Game.TimeoutPeriod));
        }
        #endregion

        #region
        [Theory, AutoData]
        public async Task TurnAsync_OnNotInitialized_Throws(int x, int y, AuthorizationToken token)
        {
            Func<Task> act = async () => await _game.TurnAsync(x, y, token);
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Theory, AutoData]
        public async Task TurnAsync_OnXOutOfGameField_Throws(int y, AuthorizationToken tokenX, AuthorizationToken tokenO)
        {
            _storeMock.Object.State = _storeMock.Object.State with { XPlayer = tokenX, OPlayer = tokenO };


            Func<Task> act = async () => await _game.TurnAsync(Game.GameSize, y, tokenX);

            await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
        }

        [Theory, AutoData]
        public async Task TurnAsync_OnYOutOfGameField_Throws(int x, AuthorizationToken tokenX, AuthorizationToken tokenO)
        {
            _storeMock.Object.State = _storeMock.Object.State with { XPlayer = tokenX, OPlayer = tokenO };

            Func<Task> act = async () => await _game.TurnAsync(x, Game.GameSize, tokenX);

            await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
        }

        [Theory, AutoData]
        public async Task TurnAsync_OnCellAlreadyUsed_Throws(int x, int y, AuthorizationToken tokenX, AuthorizationToken tokenO)
        {
            x %= Game.GameSize;
            y %= Game.GameSize;
            _storeMock.Object.State.Map[x, y] = CellStatus.X;
            _storeMock.Object.State = _storeMock.Object.State with { XPlayer = tokenX, OPlayer = tokenO };

            Func<Task> act = async () => await _game.TurnAsync(x, y, tokenX);

            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Theory, AutoData]
        public async Task TurnAsync_OnXTurn_ReturnsOTurnAndChangedMap(AuthorizationToken tokenX, AuthorizationToken tokenO)
        {
            _storeMock.Object.State = _storeMock.Object.State with { XPlayer = tokenX, OPlayer = tokenO };

            var status = await _game.TurnAsync(0, 0, tokenX);

            var gameMap = new GameMap();
            gameMap[0, 0] = CellStatus.X;
            status.Should().Be(new GameStatusDto(GameState.OTurn, gameMap));
        }

        [Theory, AutoData]
        public async Task TurnAsync_OnXTurn_StoresOTurnAndChangedMap(AuthorizationToken tokenX, AuthorizationToken tokenO)
        {
            _storeMock.Object.State = _storeMock.Object.State with { XPlayer = tokenX, OPlayer = tokenO };

            var status = await _game.TurnAsync(0, 0, tokenX);

            var gameMap = new GameMap();
            gameMap[0, 0] = CellStatus.X;
            _storeMock.Object.State.Should().Be(new GameStorageData(tokenX, tokenO, GameState.OTurn, gameMap));
            _storeMock.Verify(x => x.WriteStateAsync(), Times.Exactly(1));
        }


        [Theory, AutoData]
        public async Task TurnAsync_OnOTurn_ReturnsXTurnAndChangedMap(AuthorizationToken tokenX, AuthorizationToken tokenO)
        {
            _storeMock.Object.State = _storeMock.Object.State with { XPlayer = tokenX, OPlayer = tokenO, Status = GameState.OTurn };
            _storeMock.Object.State.Map[0, 0] = CellStatus.X;

            var status = await _game.TurnAsync(0, 1, tokenO);

            status.Status.Should().Be(GameState.XTurn, status.GameMap.ToMapString());
        }

        [Theory, AutoData]
        public async Task TurnAsync_OnOTurn_StoresXTurnAndChangedMap(AuthorizationToken tokenX, AuthorizationToken tokenO)
        {
            _storeMock.Object.State = _storeMock.Object.State with { XPlayer = tokenX, OPlayer = tokenO, Status = GameState.OTurn };
            _storeMock.Object.State.Map[0, 0] = CellStatus.X;

            var status = await _game.TurnAsync(0, 1, tokenO);

            var gameMap = new GameMap();
            gameMap[0, 0] = CellStatus.X;
            gameMap[0, 1] = CellStatus.O;
            _storeMock.Object.State.Should().Be(new GameStorageData(tokenX, tokenO, GameState.XTurn, gameMap));
            _storeMock.Verify(x => x.WriteStateAsync(), Times.Exactly(1));
        }

        [Theory, AutoData]
        public async Task TurnAsync_OnWinByHorizontallLine_DetectsWin(AuthorizationToken tokenX, AuthorizationToken tokenO)
        {
            var gameMap = new CellStatus[,]
            {
                {CellStatus.X,  CellStatus.X, CellStatus.Empty, },
                {CellStatus.O, CellStatus.O, CellStatus.Empty, },
                {CellStatus.Empty,  CellStatus.Empty, CellStatus.Empty, },
            };
            _storeMock.Object.State = _storeMock.Object.State with { XPlayer = tokenX, OPlayer = tokenO, Map = new GameMap(gameMap) };

            var status = await _game.TurnAsync(0, Game.GameSize - 1, tokenX);

            status.Status.Should().Be(GameState.XWin, status.GameMap.ToMapString());
            _storeMock.Object.State.Status.Should().Be(GameState.XWin);
        }

        [Theory, AutoData]
        public async Task TurnAsync_OnWinByVerticallLine_DetectsWin(AuthorizationToken tokenX, AuthorizationToken tokenO)
        {
            var gameMap = new CellStatus[,]
            {
                {CellStatus.X,  CellStatus.Empty, CellStatus.Empty, },
                {CellStatus.X,  CellStatus.Empty, CellStatus.Empty, },
                {CellStatus.Empty,  CellStatus.Empty, CellStatus.Empty, },
            };
            _storeMock.Object.State = _storeMock.Object.State with { XPlayer = tokenX, OPlayer = tokenO, Map = new GameMap(gameMap) };

            var status = await _game.TurnAsync(Game.GameSize - 1, 0, tokenX);

            status.Status.Should().Be(GameState.XWin, status.GameMap.ToMapString());
            _storeMock.Object.State.Status.Should().Be(GameState.XWin);
        }

        [Theory, AutoData]
        public async Task TurnAsync_OnWinByDiagonal1Line_DetectsWin(AuthorizationToken tokenX, AuthorizationToken tokenO)
        {
            var gameMap = new CellStatus[,]
            {
                {CellStatus.X,  CellStatus.Empty, CellStatus.Empty, },
                {CellStatus.Empty,  CellStatus.X, CellStatus.Empty, },
                {CellStatus.Empty,  CellStatus.Empty, CellStatus.Empty, },
            };
            _storeMock.Object.State = _storeMock.Object.State with { XPlayer = tokenX, OPlayer = tokenO, Map = new GameMap(gameMap) };

            var status = await _game.TurnAsync(Game.GameSize - 1, Game.GameSize - 1, tokenX);

            status.Status.Should().Be(GameState.XWin, status.GameMap.ToMapString());
            _storeMock.Object.State.Status.Should().Be(GameState.XWin);
        }

        [Theory, AutoData]
        public async Task TurnAsync_OnWinByDiagonal2Line_DetectsWin(AuthorizationToken tokenX, AuthorizationToken tokenO)
        {
            var gameMap = new CellStatus[,]
            {
                {CellStatus.Empty,  CellStatus.Empty, CellStatus.X, },
                {CellStatus.Empty,  CellStatus.X, CellStatus.Empty, },
                {CellStatus.Empty,  CellStatus.Empty, CellStatus.Empty, },
            };
            _storeMock.Object.State = _storeMock.Object.State with { XPlayer = tokenX, OPlayer = tokenO, Map = new GameMap(gameMap) };



            var status = await _game.TurnAsync(Game.GameSize - 1, 0, tokenX);

            status.Status.Should().Be(GameState.XWin, status.GameMap.ToMapString());
            _storeMock.Object.State.Status.Should().Be(GameState.XWin);
        }

        [Theory, AutoData]
        public async Task TurnAsync_OnXTurn_ResetsTimeoutReminder(AuthorizationToken tokenX, AuthorizationToken tokenO)
        {
            _storeMock.Object.State = _storeMock.Object.State with { XPlayer = tokenX, OPlayer = tokenO };

            await _game.TurnAsync(0, 0, tokenX);

            _mockedGame.Verify(x => x.RegisterOrUpdateReminder(Game.TimeoutCheckReminderName, Game.TimeoutPeriod, Game.TimeoutPeriod));
        }

        [Theory, AutoData]
        public async Task TurnAsync_OnYTurn_ResetsTimeoutReminder(AuthorizationToken tokenX, AuthorizationToken tokenO)
        {
            _storeMock.Object.State = _storeMock.Object.State with { XPlayer = tokenX, OPlayer = tokenO, Status = GameState.OTurn };

            await _game.TurnAsync(0, 0, tokenO);

            _mockedGame.Verify(x => x.RegisterOrUpdateReminder(Game.TimeoutCheckReminderName, Game.TimeoutPeriod, Game.TimeoutPeriod));
        }

        [Theory, AutoData]
        public async Task TurnAsync_OnValidTurn_NotifySubsribers(AuthorizationToken tokenX, AuthorizationToken tokenO, Guid grainId)
        {
            var streamMock = SetupStreamMock(grainId);

            _storeMock.Object.State = _storeMock.Object.State with { XPlayer = tokenX, OPlayer = tokenO };

            await _game.TurnAsync(0, 0, tokenX);

            var gameMap = new CellStatus[,]
            {
                {CellStatus.X,  CellStatus.Empty, CellStatus.Empty, },
                {CellStatus.Empty,  CellStatus.Empty, CellStatus.Empty, },
                {CellStatus.Empty,  CellStatus.Empty, CellStatus.Empty, },
            };
            streamMock.Verify(x => x.OnNextAsync(new GameStatusDto(GameState.OTurn, new GameMap(gameMap)), null), Times.Once);
        }
        #endregion

        #region
        [Theory]
        [InlineAutoData(GameState.TimedOut)]
        [InlineAutoData(GameState.XWin)]
        [InlineAutoData(GameState.OWin)]
        public async Task ReceiveReminder_OnGameInEndState_CancelsReminder(GameState gameState)
        {
            var reminderMock = new Mock<IGrainReminder>();
            _mockedGame.Setup(x => x.GetReminder(Game.TimeoutCheckReminderName)).ReturnsAsync(reminderMock.Object);
            var _game = _mockedGame.Object;
            _storeMock.Object.State = _storeMock.Object.State with { Status = gameState };

            await _game.ReceiveReminder(Game.TimeoutCheckReminderName, new TickStatus());

            _storeMock.Object.State.Status.Should().Be(gameState);
            _mockedGame.Verify(x => x.UnregisterReminder(reminderMock.Object), Times.Once);

        }

        [Theory]
        [InlineAutoData(GameState.OTurn)]
        [InlineAutoData(GameState.XTurn)]
        public async Task ReceiveReminder_OnGameInNotEndState_EndsGame(GameState gameState)
        {
            var reminderMock = new Mock<IGrainReminder>();
            _mockedGame.Setup(x => x.GetReminder(Game.TimeoutCheckReminderName)).ReturnsAsync(reminderMock.Object);
            _storeMock.Object.State = _storeMock.Object.State with { Status = gameState };

            await _game.ReceiveReminder(Game.TimeoutCheckReminderName, new TickStatus());

            _storeMock.Object.State.Status.Should().Be(GameState.TimedOut);
            _storeMock.Verify(x => x.WriteStateAsync(), Times.Once);
        }

        [Theory]
        [InlineAutoData(GameState.OTurn)]
        [InlineAutoData(GameState.XTurn)]
        public async Task ReceiveReminder_OnGameInNotEndState_CancelsReminder(GameState gameState)
        {
            var reminderMock = new Mock<IGrainReminder>();
            _mockedGame.Setup(x => x.GetReminder(Game.TimeoutCheckReminderName)).ReturnsAsync(reminderMock.Object);
            _storeMock.Object.State = _storeMock.Object.State with { Status = gameState };

            await _game.ReceiveReminder(Game.TimeoutCheckReminderName, new TickStatus());

            _mockedGame.Verify(x => x.UnregisterReminder(reminderMock.Object), Times.Once);
        }

        [Theory]
        [InlineAutoData(GameState.OTurn)]
        [InlineAutoData(GameState.XTurn)]
        public async Task ReceiveReminder_OnGameNotInEndState_NotifyObservers(GameState gameState, Guid grainId)
        {
            var streamMock = SetupStreamMock(grainId);
            var _game = _mockedGame.Object;
            _storeMock.Object.State = _storeMock.Object.State with { Status = gameState };

            await _game.ReceiveReminder(Game.TimeoutCheckReminderName, new TickStatus());

            streamMock.Verify(x => x.OnNextAsync(new GameStatusDto(GameState.TimedOut, new GameMap()), null), Times.Once);
        }
        #endregion
    }
}
