using OrleanPG.Grains.Interfaces;
using Orleans;
using Orleans.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OrleanPG.Grains.GameLobbyGrain
{
    public class GameLobby : Grain, IGameLobby
    {
        private readonly IPersistentState<GamesStorageState> _gameStates;
        private readonly IPersistentState<UserStates> _userStates;

        public GameLobby(
              [PersistentState("game_lobby_game_states", "game_states_store")] IPersistentState<GamesStorageState> gameStates,
              [PersistentState("game_lobby_user_states", "user_states_store")] IPersistentState<UserStates> userStates)
        {
            _gameStates = gameStates;
            _userStates = userStates;
        }

        /// <summary>
        /// NOTE: Required for unit tests
        /// </summary>
        public new virtual IGrainFactory GrainFactory => base.GrainFactory;

        public async Task<AuthorizationToken> AuthorizeAsync(string username)
        {
            var token = new AuthorizationToken(Guid.NewGuid().ToString());
            _userStates.State.AuthorizedUsers[token] = username ?? throw new ArgumentNullException();
            await _userStates.WriteStateAsync();
            return token;
        }

        public async Task<GameId> CreateGameAsync(AuthorizationToken authToken, bool playForX)
        {
            ThrowIfUserTokenIsNotValid(authToken);
            var gameId = new GameId(Guid.NewGuid());
            _gameStates.State.RegisteredGames[gameId] = playForX ? new GameParticipation(authToken, null) : new GameParticipation(null, authToken);
            await _gameStates.WriteStateAsync();
            return gameId;
        }

        private void ThrowIfUserTokenIsNotValid(AuthorizationToken authToken)
        {
            if (!_userStates.State.AuthorizedUsers.ContainsKey(authToken))
            {
                throw new ArgumentException("User token is not valid");
            }
        }

        public async Task<bool> JoinGameAsync(AuthorizationToken authToken, GameId id)
        {
            ThrowIfUserTokenIsNotValid(authToken);
            if (id == null)
            {
                throw new ArgumentNullException();
            }
            if (!_gameStates.State.RegisteredGames.ContainsKey(id))
            {
                throw new ArgumentException();
            }

            var gameData = _gameStates.State.RegisteredGames[id];
            if (gameData == null)
            {
                throw new ArgumentException($"Game not found: {id}");
            }

            gameData = gameData.JoinPlayer(authToken, out var playForX);
            _gameStates.State.RegisteredGames[id] = gameData;
            await _gameStates.WriteStateAsync();
            var init = GrainFactory.GetGrain<IGameInitializer>(id.Value);
#pragma warning disable CS8604 // Possible null reference argument.
            await init.StartAsync(gameData.XPlayer, gameData.OPlayer);
#pragma warning restore CS8604 // Possible null reference argument.
            return playForX;

        }

        public Task<GameListItemDto[]> FindGamesAsync()
        {
            var result = _gameStates.State.RegisteredGames
                .Select(x => new GameListItemDto() { Id = x.Key, XPlayerName = TryGetUserName(x.Value.XPlayer), OPlayerName = TryGetUserName(x.Value.OPlayer) })
                .ToArray();
            return Task.FromResult(result);
        }

        private string? TryGetUserName(AuthorizationToken? token) => token == null ? null : _userStates.State.AuthorizedUsers[token];
    }
}
