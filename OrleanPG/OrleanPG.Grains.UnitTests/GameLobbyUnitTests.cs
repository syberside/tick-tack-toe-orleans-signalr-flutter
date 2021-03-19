using AutoFixture;
using FluentAssertions;
using OrleanPG.Grains.Interfaces;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace OrleanPG.Grains.UnitTests
{
    public class GameLobbyUnitTests
    {
        private readonly IGameLobby _lobby = new GameLobby();
        private readonly Fixture _fixture = new Fixture();

        [Fact]
        public async Task AuthorizeAsync_OnNotNullUserName_ReturnsUserToken()
        {
            var token = await _lobby.AuthorizeAsync(_fixture.Create<string>());
            token.Should().NotBeNull();
        }

        [Fact]
        public async Task AuthorizeAsync_OnNullUserName_Throws()
        {
            Func<Task> act = async () => await _lobby.AuthorizeAsync(null);
            await act.Should().ThrowAsync<ArgumentNullException>();
        }

        [Fact]
        public async Task CreateNewAsync_OnNotNullUserToken_ReturnsGameToken()
        {
            var userToken = await _lobby.AuthorizeAsync(_fixture.Create<string>());
            var gameToken = await _lobby.CreateNewAsync(userToken, _fixture.Create<bool>());
            gameToken.Should().NotBeNull();
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
        public async Task JoinGame_OnValidUserTokenAndGameId_ReturnsValidGameToken()
        {
            var userToken1 = await _lobby.AuthorizeAsync(_fixture.Create<string>());
            var gameToken1 = await _lobby.CreateNewAsync(userToken1, _fixture.Create<bool>());
            var userToken2 = await _lobby.AuthorizeAsync(_fixture.Create<string>());
            var gamesInfo = await _lobby.FindGamesAsync();
            var gameInfo = gamesInfo.Single();

            var gameToken2 = await _lobby.JoinGameAsync(userToken2, gameInfo.Id);

            gameToken1.Should().Be(gameToken2);
        }

        [Fact]
        public async Task JoinGame_OnValidUserTokenAndGameId_MarksGameAsStarted()
        {
            var userToken1 = await _lobby.AuthorizeAsync(_fixture.Create<string>());
            await _lobby.CreateNewAsync(userToken1, _fixture.Create<bool>());
            var userToken2 = await _lobby.AuthorizeAsync(_fixture.Create<string>());
            var gamesInfo = await _lobby.FindGamesAsync();
            var gameInfo = gamesInfo.Single();
            await _lobby.JoinGameAsync(userToken2, gameInfo.Id);

            gamesInfo = await _lobby.FindGamesAsync();
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
