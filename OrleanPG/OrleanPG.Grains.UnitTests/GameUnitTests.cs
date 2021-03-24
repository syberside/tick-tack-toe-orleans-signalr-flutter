using AutoFixture.Xunit2;
using FluentAssertions;
using Moq;
using OrleanPG.Grains.GameGrain;
using OrleanPG.Grains.GameLobbyGrain.UnitTests.Helpers;
using OrleanPG.Grains.Interfaces;
using Orleans.Runtime;
using System;
using System.Threading.Tasks;
using Xunit;

namespace OrleanPG.Grains.UnitTests
{
    public class GameUnitTests
    {
        private readonly Mock<IPersistentState<GameStorageData>> _storeMock;
        private readonly Game _game;

        public GameUnitTests()
        {
            _storeMock = PersistanceHelper.CreateAndSetupStateWriteMock<GameStorageData>();
            _game = new Game(_storeMock.Object);
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
            _storeMock.Object.State.Map[x, y] = true;
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
            gameMap[0, 0] = true;
            status.Should().Be(new GameStatusDto(GameState.OTurn, gameMap));
        }

        [Theory, AutoData]
        public async Task TurnAsync_OnXTurn_StoresOTurnAndChangedMap(AuthorizationToken tokenX, AuthorizationToken tokenO)
        {
            _storeMock.Object.State = _storeMock.Object.State with { XPlayer = tokenX, OPlayer = tokenO };

            var status = await _game.TurnAsync(0, 0, tokenX);

            var gameMap = new GameMap();
            gameMap[0, 0] = true;
            _storeMock.Object.State.Should().Be(new GameStorageData(tokenX, tokenO, GameState.OTurn, gameMap));
            _storeMock.Verify(x => x.WriteStateAsync(), Times.Exactly(1));
        }



        [Theory, AutoData]
        public async Task TurnAsync_OnOTurn_ReturnsXTurnAndChangedMap(AuthorizationToken tokenX, AuthorizationToken tokenO)
        {
            _storeMock.Object.State = _storeMock.Object.State with { XPlayer = tokenX, OPlayer = tokenO, Status = GameState.OTurn };
            _storeMock.Object.State.Map[0, 0] = true;

            var status = await _game.TurnAsync(0, 1, tokenO);

            status.Status.Should().Be(GameState.XTurn, status.GameMap.ToMapString());
        }

        [Theory, AutoData]
        public async Task TurnAsync_OnOTurn_StoresXTurnAndChangedMap(AuthorizationToken tokenX, AuthorizationToken tokenO)
        {
            _storeMock.Object.State = _storeMock.Object.State with { XPlayer = tokenX, OPlayer = tokenO, Status = GameState.OTurn };
            _storeMock.Object.State.Map[0, 0] = true;

            var status = await _game.TurnAsync(0, 1, tokenO);

            var gameMap = new GameMap();
            gameMap[0, 0] = true;
            gameMap[0, 1] = false;
            _storeMock.Object.State.Should().Be(new GameStorageData(tokenX, tokenO, GameState.XTurn, gameMap));
            _storeMock.Verify(x => x.WriteStateAsync(), Times.Exactly(1));
        }

        [Theory, AutoData]
        public async Task TurnAsync_OnWinByHorizontallLine_DetectsWin(AuthorizationToken tokenX, AuthorizationToken tokenO)
        {
            var gameMap = new bool?[,]
            {
                {true,  true, null, },
                {false, false, null, },
                {null,  null, null, },
            };
            _storeMock.Object.State = _storeMock.Object.State with { XPlayer = tokenX, OPlayer = tokenO, Map = new GameMap(gameMap) };

            var status = await _game.TurnAsync(0, Game.GameSize - 1, tokenX);

            status.Status.Should().Be(GameState.XWin, status.GameMap.ToMapString());
            _storeMock.Object.State.Status.Should().Be(GameState.XWin);
        }

        [Theory, AutoData]
        public async Task TurnAsync_OnWinByVerticallLine_DetectsWin(AuthorizationToken tokenX, AuthorizationToken tokenO)
        {
            var gameMap = new bool?[,]
{
                {true,  null, null, },
                {true, null, null, },
                {null,  null, null, },
};
            _storeMock.Object.State = _storeMock.Object.State with { XPlayer = tokenX, OPlayer = tokenO, Map = new GameMap(gameMap) };

            var status = await _game.TurnAsync(Game.GameSize - 1, 0, tokenX);

            status.Status.Should().Be(GameState.XWin, status.GameMap.ToMapString());
            _storeMock.Object.State.Status.Should().Be(GameState.XWin);
        }

        [Theory, AutoData]
        public async Task TurnAsync_OnWinByDiagonal1Line_DetectsWin(AuthorizationToken tokenX, AuthorizationToken tokenO)
        {
            var gameMap = new bool?[,]
{
                {true,  null, null, },
                {null, true, null, },
                {null,  null, null, },
};
            _storeMock.Object.State = _storeMock.Object.State with { XPlayer = tokenX, OPlayer = tokenO, Map = new GameMap(gameMap) };

            var status = await _game.TurnAsync(Game.GameSize - 1, Game.GameSize - 1, tokenX);

            status.Status.Should().Be(GameState.XWin, status.GameMap.ToMapString());
            _storeMock.Object.State.Status.Should().Be(GameState.XWin);
        }

        [Theory, AutoData]
        public async Task TurnAsync_OnWinByDiagonal2Line_DetectsWin(AuthorizationToken tokenX, AuthorizationToken tokenO)
        {
            var gameMap = new bool?[,]
            {
                {null,  null, true, },
                {null, true, null, },
                {null,  null, null, },
            };
            _storeMock.Object.State = _storeMock.Object.State with { XPlayer = tokenX, OPlayer = tokenO, Map = new GameMap(gameMap) };



            var status = await _game.TurnAsync(Game.GameSize - 1, 0, tokenX);

            status.Status.Should().Be(GameState.XWin, status.GameMap.ToMapString());
            _storeMock.Object.State.Status.Should().Be(GameState.XWin);
        }
        #endregion
    }
}
