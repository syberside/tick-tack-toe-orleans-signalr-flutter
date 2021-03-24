using AutoFixture;
using FluentAssertions;
using Moq;
using OrleanPG.Grains.Interfaces;
using Orleans;
using Orleans.Runtime;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using OrleanPG.Grains.GameLobbyGrain.UnitTests.Helpers;
using AutoFixture.Xunit2;

namespace OrleanPG.Grains.GameLobbyGrain.UnitTests
{
    /// <summary>
    /// TODO: Because state providers were added tests should be refactored to presetup state and dont use SUT methods to modify data.
    /// </summary>
    public class GameLobbyUnitTests
    {
        private readonly IGameLobby _lobby;
        private readonly Fixture _fixture;
        private readonly Mock<IPersistentState<GamesStorageState>> _gamesStateMock;
        private readonly Mock<IPersistentState<UserStates>> _userStatesMock;


        public GameLobbyUnitTests()
        {
            _fixture = new();
            _gamesStateMock = PersistanceHelper.CreateAndSetupStateWriteMock<GamesStorageState>();
            _userStatesMock = PersistanceHelper.CreateAndSetupStateWriteMock<UserStates>();

            _lobby = new GameLobby(_gamesStateMock.Object, _userStatesMock.Object);
        }

        [Fact]
        public async Task AuthorizeAsync_OnNotNullUserName_ReturnsUserToken()
        {
            string username = _fixture.Create<string>();
            var token = await _lobby.AuthorizeAsync(username);
            token.Should().NotBeNull();
            _userStatesMock.Object.State.AuthorizedUsers[token].Should().Be(username);
            _userStatesMock.Verify(x => x.WriteStateAsync(), Times.Once);
        }

        [Fact]
        public async Task AuthorizeAsync_OnNullUserName_Throws()
        {
            Func<Task> act = async () => await _lobby.AuthorizeAsync(null);
            await act.Should().ThrowAsync<ArgumentNullException>();
        }

        [Fact]
        public async Task CreateNewAsync_OnNotNullUserToken_ReturnsGameId()
        {
            var userToken = await _lobby.AuthorizeAsync(_fixture.Create<string>());
            var gameId = await _lobby.CreateNewAsync(userToken, _fixture.Create<bool>());

            gameId.Should().NotBeNull();
        }

        [Theory, AutoData]
        public async Task CreateNewAsync_OnUserWantsToBeX_StoresNewGame(string username)
        {
            var userToken = await _lobby.AuthorizeAsync(username);
            var gameId = await _lobby.CreateNewAsync(userToken, true);

            gameId.Should().NotBeNull();
            _gamesStateMock.Object.State.RegisteredGames[gameId].Should().Be(new GameParticipation(userToken, null));
            _gamesStateMock.Verify(x => x.WriteStateAsync(), Times.Once);
        }

        [Theory, AutoData]
        public async Task CreateNewAsync_OnUserWantsToBeO_StoresNewGame(string username)
        {
            var userToken = await _lobby.AuthorizeAsync(username);
            var gameId = await _lobby.CreateNewAsync(userToken, false);

            gameId.Should().NotBeNull();
            _gamesStateMock.Object.State.RegisteredGames[gameId].Should().Be(new GameParticipation(null, userToken));
            _gamesStateMock.Verify(x => x.WriteStateAsync(), Times.Once);
        }

        [Fact]
        public async Task CreateNewAsync_OnNullUserToken_Throws()
        {
            Func<Task> act = async () => await _lobby.CreateNewAsync(null, _fixture.Create<bool>());
            await act.Should().ThrowAsync<ArgumentException>();
        }

        [Fact]
        public async Task CreateNewAsync_OnPlayForXTrue_CreatesGameSessionWithFilledXPLayer()
        {
            var username = _fixture.Create<string>();
            var userToken = await _lobby.AuthorizeAsync(username);
            await _lobby.CreateNewAsync(userToken, true);
            var gamesInfo = await _lobby.FindGamesAsync();
            var gameInfo = gamesInfo.Single();

            gameInfo.XPlayerName.Should().Be(username);
            gameInfo.OPlayerName.Should().BeNull();
        }


        [Fact]
        public async Task CreateNewAsync_OnPlayForXFals_CreatesGameSessionWithFilledOPLayer()
        {
            var username = _fixture.Create<string>();
            var userToken = await _lobby.AuthorizeAsync(username);
            await _lobby.CreateNewAsync(userToken, false);
            var gamesInfo = await _lobby.FindGamesAsync();
            var gameInfo = gamesInfo.Single();

            gameInfo.OPlayerName.Should().Be(username);
            gameInfo.XPlayerName.Should().BeNull();
        }

        [Fact]
        public async Task JoinGame_OnJoiningAsO_StoresUser()
        {
            var mocked = new Mock<GameLobby>(() => new GameLobby(_gamesStateMock.Object, _userStatesMock.Object));

            var lobby = mocked.Object;

            var usernameX = _fixture.Create<string>();
            var userToken1 = await lobby.AuthorizeAsync(usernameX);
            var createdGame = await lobby.CreateNewAsync(userToken1, true);
            var usernameO = _fixture.Create<string>();
            var userToken2 = await lobby.AuthorizeAsync(usernameO);

            var factoryMock = new Mock<IGrainFactory>();
            var initializerMock = new Mock<IGameInitializer>();
            factoryMock.Setup(x => x.GetGrain<IGameInitializer>(createdGame.Value, null)).Returns(initializerMock.Object);
            mocked.Setup(x => x.GrainFactory).Returns(factoryMock.Object);
            await lobby.JoinGameAsync(userToken2, createdGame);

            _gamesStateMock.Object.State.RegisteredGames[createdGame].Should().Be(new GameParticipation(userToken1, userToken2));
        }

        [Fact]
        public async Task JoinGame_OnJoiningAsX_StoresUser()
        {
            var mocked = new Mock<GameLobby>(() => new GameLobby(_gamesStateMock.Object, _userStatesMock.Object));

            var lobby = mocked.Object;

            var usernameX = _fixture.Create<string>();
            var userToken1 = await lobby.AuthorizeAsync(usernameX);
            var createdGame = await lobby.CreateNewAsync(userToken1, false);
            var usernameO = _fixture.Create<string>();
            var userToken2 = await lobby.AuthorizeAsync(usernameO);

            var factoryMock = new Mock<IGrainFactory>();
            var initializerMock = new Mock<IGameInitializer>();
            factoryMock.Setup(x => x.GetGrain<IGameInitializer>(createdGame.Value, null)).Returns(initializerMock.Object);
            mocked.Setup(x => x.GrainFactory).Returns(factoryMock.Object);
            await lobby.JoinGameAsync(userToken2, createdGame);

            _gamesStateMock.Object.State.RegisteredGames[createdGame].Should().Be(new GameParticipation(userToken2, userToken1));
        }

        [Fact]
        public async Task JoinGame_OnValidUserTokenAndGameId_StoresUserAndRunGame()
        {
            var mocked = new Mock<GameLobby>(() => new GameLobby(_gamesStateMock.Object, _userStatesMock.Object));

            var lobby = mocked.Object;

            var usernameX = _fixture.Create<string>();
            var userToken1 = await lobby.AuthorizeAsync(usernameX);
            var createdGame = await lobby.CreateNewAsync(userToken1, _fixture.Create<bool>());
            var usernameO = _fixture.Create<string>();
            var userToken2 = await lobby.AuthorizeAsync(usernameO);

            var factoryMock = new Mock<IGrainFactory>();
            var initializerMock = new Mock<IGameInitializer>();
            factoryMock.Setup(x => x.GetGrain<IGameInitializer>(createdGame.Value, null)).Returns(initializerMock.Object);
            mocked.Setup(x => x.GrainFactory).Returns(factoryMock.Object);
            await lobby.JoinGameAsync(userToken2, createdGame);

            var gamesInfo = await lobby.FindGamesAsync();
            var gameInfo = gamesInfo.Single();
            gameInfo.Should().Be(new GameListItemDto() { Id = createdGame, XPlayerName = usernameX, OPlayerName = usernameO, });
            initializerMock.Verify(x => x.StartAsync(userToken1, userToken2), Times.Once);
        }

        [Fact]
        public async Task JoinGame_OnValidUserTokenAndGameId_MarksGameAsStarted()
        {
            var mocked = new Mock<GameLobby>(() => new GameLobby(_gamesStateMock.Object, _userStatesMock.Object));
            var lobby = mocked.Object;

            var userToken1 = await lobby.AuthorizeAsync(_fixture.Create<string>());
            var createdGame = await lobby.CreateNewAsync(userToken1, _fixture.Create<bool>());
            var userToken2 = await lobby.AuthorizeAsync(_fixture.Create<string>());
            var gamesInfo = await lobby.FindGamesAsync();
            var gameInfo = gamesInfo.Single();

            var factoryMock = new Mock<IGrainFactory>();
            var initializerMock = new Mock<IGameInitializer>();
            factoryMock.Setup(x => x.GetGrain<IGameInitializer>(createdGame.Value, null)).Returns(initializerMock.Object);
            mocked.Setup(x => x.GrainFactory).Returns(factoryMock.Object);

            await lobby.JoinGameAsync(userToken2, gameInfo.Id);

            gamesInfo = await lobby.FindGamesAsync();
            gameInfo = gamesInfo.Single();

            gameInfo.IsRunning.Should().BeTrue();
        }

        [Fact]
        public async Task JoinGame_OnSameUserJoining_Throws()
        {
            var userToken = await _lobby.AuthorizeAsync(_fixture.Create<string>());
            await _lobby.CreateNewAsync(userToken, _fixture.Create<bool>());
            var gamesInfo = await _lobby.FindGamesAsync();
            var gameInfo = gamesInfo.Single();

            Func<Task> act = async () => await _lobby.JoinGameAsync(userToken, gameInfo.Id);
            await act.Should().ThrowAsync<ArgumentException>();
        }

        [Fact]
        public async Task JoinGame_OnNullUserJoining_Throws()
        {
            var userToken = await _lobby.AuthorizeAsync(_fixture.Create<string>());
            await _lobby.CreateNewAsync(userToken, _fixture.Create<bool>());
            var gamesInfo = await _lobby.FindGamesAsync();
            var gameInfo = gamesInfo.Single();

            Func<Task> act = async () => await _lobby.JoinGameAsync(null, gameInfo.Id);
            await act.Should().ThrowAsync<ArgumentException>();
        }

        [Fact]
        public async Task JoinGame_OnNullGameId_Throws()
        {
            var userToken = await _lobby.AuthorizeAsync(_fixture.Create<string>());
            await _lobby.CreateNewAsync(userToken, _fixture.Create<bool>());
            var gamesInfo = await _lobby.FindGamesAsync();
            var gameInfo = gamesInfo.Single();

            Func<Task> act = async () => await _lobby.JoinGameAsync(userToken, null);
            await act.Should().ThrowAsync<ArgumentException>();
        }

        [Fact]
        public async Task JoinGame_OnInvalidGameId_Throws()
        {
            var userToken = await _lobby.AuthorizeAsync(_fixture.Create<string>());
            await _lobby.CreateNewAsync(userToken, _fixture.Create<bool>());
            var gamesInfo = await _lobby.FindGamesAsync();
            var gameInfo = gamesInfo.Single();

            Func<Task> act = async () => await _lobby.JoinGameAsync(userToken, _fixture.Create<GameId>());
            await act.Should().ThrowAsync<ArgumentException>();
        }

        [Fact]
        public async Task CreateGame_OnInvalidUserToken_Throws()
        {
            Func<Task> act = async () => await _lobby.CreateNewAsync(_fixture.Create<AuthorizationToken>(), _fixture.Create<bool>());
            await act.Should().ThrowAsync<ArgumentException>();
        }
    }
}
