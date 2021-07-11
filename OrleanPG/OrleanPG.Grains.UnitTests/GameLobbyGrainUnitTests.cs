using AutoFixture;
using AutoFixture.Xunit2;
using FluentAssertions;
using Moq;
using OrleanPG.Grains.GameLobbyGrain.UnitTests.Helpers;
using OrleanPG.Grains.Interfaces;
using OrleanPG.Grains.Interfaces.Dtos;
using Orleans;
using Orleans.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace OrleanPG.Grains.GameLobbyGrain.UnitTests
{
    public class GameLobbyGrainUnitTests
    {
        private readonly IGameLobby _lobby;
        private readonly Mock<IPersistentState<GamesStorageState>> _gamesStateMock;
        private readonly Mock<IPersistentState<UserStates>> _userStatesMock;


        public GameLobbyGrainUnitTests()
        {
            _gamesStateMock = PersistanceHelper.CreateAndSetupStateWriteMock<GamesStorageState>();
            _userStatesMock = PersistanceHelper.CreateAndSetupStateWriteMock<UserStates>();

            _lobby = new GameLobbyGrain(_gamesStateMock.Object, _userStatesMock.Object);
        }

        #region AuthorizeAsync
        [Theory, AutoData]
        public async Task AuthorizeAsync_OnNotNullUserName_ReturnsUserToken(string username)
        {
            var token = await _lobby.AuthorizeAsync(username);

            token.Should().NotBeNull();
        }

        [Theory, AutoData]
        public async Task AuthorizeAsync_OnNotNullUserName_StoresUserData(string username)
        {
            var token = await _lobby.AuthorizeAsync(username);

            _userStatesMock.Object.State.AuthorizedUsers[token].Should().Be(username);
            _userStatesMock.Verify(x => x.WriteStateAsync(), Times.Once);
        }

        [Fact]
        public async Task AuthorizeAsync_OnNullUserName_Throws()
        {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            Func<Task> act = async () => await _lobby.AuthorizeAsync(null);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

            await act.Should().ThrowAsync<ArgumentNullException>();
        }
        #endregion

        #region CreateGameAsync
        [Theory, AutoData]
        public async Task CreateGameAsync_OnNotNullUserToken_ReturnsGameId(string username, bool playForX)
        {
            var userToken = await _lobby.AuthorizeAsync(username);
            var gameId = await _lobby.CreateGameAsync(userToken, playForX);

            gameId.Should().NotBeNull();
        }

        [Theory, AutoData]
        public async Task CreateGameAsync_OnUserWantsToBeX_StoresNewGame(string username)
        {
            var userToken = await _lobby.AuthorizeAsync(username);

            var gameId = await _lobby.CreateGameAsync(userToken, true);

            _gamesStateMock.Object.State.RegisteredGames[gameId].Should().Be(new GameParticipation(userToken, null));
            _gamesStateMock.Verify(x => x.WriteStateAsync(), Times.Once);
        }

        [Theory, AutoData]
        public async Task CreateGameAsync_OnUserWantsToBeO_StoresNewGame(string username)
        {
            var userToken = await _lobby.AuthorizeAsync(username);

            var gameId = await _lobby.CreateGameAsync(userToken, false);

            _gamesStateMock.Object.State.RegisteredGames[gameId].Should().Be(new GameParticipation(null, userToken));
            _gamesStateMock.Verify(x => x.WriteStateAsync(), Times.Once);
        }

        [Theory, AutoData]
        public async Task CreateGameAsync_OnNullUserToken_Throws(bool playForX)
        {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            Func<Task> act = async () => await _lobby.CreateGameAsync(null, playForX);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

            await act.Should().ThrowAsync<ArgumentException>();
        }

        [Theory, AutoData]
        public async Task CreateGame_OnInvalidUserToken_Throws(AuthorizationToken token, bool playForX)
        {
            Func<Task> act = async () => await _lobby.CreateGameAsync(token, playForX);
            await act.Should().ThrowAsync<ArgumentException>();
        }
        #endregion

        #region JoinGame
        private GameLobbyGrain CreateWrappedLobbyWithGrainInitializerSetup(GameId gameId, out Mock<IGameInitializer> initializerMock)
        {
            var wrappedLobby = new Mock<GameLobbyGrain>(() => new GameLobbyGrain(_gamesStateMock.Object, _userStatesMock.Object));
            var lobby = wrappedLobby.Object;
            var factoryMock = new Mock<IGrainFactory>();
            initializerMock = new Mock<IGameInitializer>();
            factoryMock.Setup(x => x.GetGrain<IGameInitializer>(gameId.Value, null)).Returns(initializerMock.Object);
            wrappedLobby.Setup(x => x.GrainFactory).Returns(factoryMock.Object);
            return lobby;
        }

        [Theory, AutoData]
        public async Task JoinGameAsync_OnValidUserTokenAndGameId_StoresUser(AuthorizationToken tokenX, string usernameX, AuthorizationToken tokenO, string usernameO, GameId gameId)
        {
            var lobby = CreateWrappedLobbyWithGrainInitializerSetup(gameId, out _);
            _userStatesMock.Object.State.AuthorizedUsers.Add(tokenX, usernameX);
            _gamesStateMock.Object.State.RegisteredGames.Add(gameId, new GameParticipation(tokenX, null));
            _userStatesMock.Object.State.AuthorizedUsers.Add(tokenO, usernameO);

            await lobby.JoinGameAsync(tokenO, gameId);

            _gamesStateMock.Object.State.RegisteredGames[gameId].Should().Be(new GameParticipation(tokenX, tokenO));
        }

        [Theory, AutoData]
        public async Task JoinGameAsync_OnValidUserTokenAndGameId_RunsGame(AuthorizationToken tokenX, string usernameX, AuthorizationToken tokenO, string usernameO, GameId gameId)
        {
            var lobby = CreateWrappedLobbyWithGrainInitializerSetup(gameId, out Mock<IGameInitializer> initializerMock);
            _userStatesMock.Object.State.AuthorizedUsers.Add(tokenX, usernameX);
            _gamesStateMock.Object.State.RegisteredGames.Add(gameId, new GameParticipation(tokenX, null));
            _userStatesMock.Object.State.AuthorizedUsers.Add(tokenO, usernameO);

            await lobby.JoinGameAsync(tokenO, gameId);

            initializerMock.Verify(x => x.StartAsync(tokenX, tokenO), Times.Once);
        }

        [Theory, AutoData]
        public async Task JoinGameAsync_OnValidUserTokenAndGameId_ReturnsInitialGameStatus(
            AuthorizationToken tokenX, string usernameX,
            AuthorizationToken tokenO, string usernameO,
            GameId gameId)
        {

            var lobby = CreateWrappedLobbyWithGrainInitializerSetup(gameId, out var gameInitializerMock);
            _userStatesMock.Object.State.AuthorizedUsers.Add(tokenX, usernameX);
            _gamesStateMock.Object.State.RegisteredGames.Add(gameId, new GameParticipation(tokenX, null));
            _userStatesMock.Object.State.AuthorizedUsers.Add(tokenO, usernameO);
            var expected = new Fixture().Create<GameStatusDto>();
            gameInitializerMock.Setup(x => x.StartAsync(tokenX, tokenO)).ReturnsAsync(expected);

            var result = await lobby.JoinGameAsync(tokenO, gameId);

            result.Should().Be(expected);
        }

        [Theory, AutoData]
        public async Task JoinGameAsync_OnSameUserJoining_Throws(AuthorizationToken tokenX, string usernameX, GameId gameId)
        {
            _userStatesMock.Object.State.AuthorizedUsers.Add(tokenX, usernameX);
            _gamesStateMock.Object.State.RegisteredGames.Add(gameId, new GameParticipation(tokenX, null));

            Func<Task> act = async () => await _lobby.JoinGameAsync(tokenX, gameId);
            await act.Should().ThrowAsync<ArgumentException>();
        }

        [Theory, AutoData]
        public async Task JoinGameAsync_OnNullUserJoining_Throws(AuthorizationToken tokenX, string usernameX, GameId gameId)
        {
            _userStatesMock.Object.State.AuthorizedUsers.Add(tokenX, usernameX);
            _gamesStateMock.Object.State.RegisteredGames.Add(gameId, new GameParticipation(tokenX, null));

#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            Func<Task> act = async () => await _lobby.JoinGameAsync(null, gameId);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
            await act.Should().ThrowAsync<ArgumentException>();
        }

        [Theory, AutoData]
        public async Task JoinGameAsync_OnNullGameId_Throws(AuthorizationToken tokenX, string usernameX, AuthorizationToken tokenO, GameId gameId)
        {
            _userStatesMock.Object.State.AuthorizedUsers.Add(tokenX, usernameX);
            _gamesStateMock.Object.State.RegisteredGames.Add(gameId, new GameParticipation(tokenX, null));

#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            Func<Task> act = async () => await _lobby.JoinGameAsync(tokenO, null);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
            await act.Should().ThrowAsync<ArgumentException>();
        }

        [Theory, AutoData]
        public async Task JoinGameAsync_OnInvalidGameId_Throws(
            AuthorizationToken tokenX, string usernameX, AuthorizationToken tokenO, string usernameO,
            GameId gameId, GameId invalidGameId)
        {
            _userStatesMock.Object.State.AuthorizedUsers.Add(tokenX, usernameX);
            _userStatesMock.Object.State.AuthorizedUsers.Add(tokenO, usernameO);
            _gamesStateMock.Object.State.RegisteredGames.Add(gameId, new GameParticipation(tokenX, null));

            Func<Task> act = async () => await _lobby.JoinGameAsync(tokenO, invalidGameId);
            await act.Should().ThrowAsync<ArgumentException>();
        }
        #endregion

        #region FindGamesAsync
        [Theory, AutoData]
        public async Task FindGamesAsync_Always_ReturnValidGamesList(Dictionary<AuthorizationToken, string> users, GameId[] games)
        {
            users.Should().HaveCount(3);
            games.Should().HaveCount(3);

            _userStatesMock.Object.State = _userStatesMock.Object.State with { AuthorizedUsers = users };
            var userTokens = users.Keys.ToArray();
            var userNames = users.Values.ToArray();
            _gamesStateMock.Object.State.RegisteredGames.Add(games[0], new GameParticipation(userTokens[0], null));
            _gamesStateMock.Object.State.RegisteredGames.Add(games[1], new GameParticipation(null, userTokens[1]));
            _gamesStateMock.Object.State.RegisteredGames.Add(games[2], new GameParticipation(userTokens[2], userTokens[0]));

            var result = await _lobby.FindGamesAsync();

            var expected = new GameListItemDto[]
            {
                new GameListItemDto{Id = games[0], XPlayerName = userNames[0]},
                new GameListItemDto{Id = games[1], OPlayerName = userNames[1]},
                new GameListItemDto{Id = games[2], XPlayerName = userNames[2], OPlayerName = userNames[0]},

            };
            result.Should().BeEquivalentTo(expected);
        }
        #endregion
    }
}
