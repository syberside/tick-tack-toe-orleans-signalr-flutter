using OrleanPG.Grains.Interfaces;
using Orleans;
using Orleans.Runtime;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace OrleanPG.Grains.GameLobbyGrain
{
    public class GameLobbyGrain : Grain, IGameLobby
    {
        private readonly IPersistentState<GamesStorageState> _gameStates;
        private readonly IPersistentState<UserStates> _userStates;
        public GameLobbyGrain(
                [PersistentState("game_lobby_game_states", "game_states_store")]
                IPersistentState<GamesStorageState> gameStates,
                [PersistentState("game_lobby_user_states", "user_states_store")]
                IPersistentState<UserStates> userStates)
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

        public async Task<GameStatusDto> JoinGameAsync(AuthorizationToken authToken, GameId id)
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

            gameData = gameData.JoinPlayer(authToken, out var _);
            _gameStates.State.RegisteredGames[id] = gameData;
            await _gameStates.WriteStateAsync();
            var gameInitializer = GrainFactory.GetGrain<IGameInitializer>(id.Value);
#pragma warning disable CS8604 // Possible null reference argument.
            var result = await gameInitializer.StartAsync(gameData.XPlayer, gameData.OPlayer);
#pragma warning restore CS8604 // Possible null reference argument.
            return result;

        }

        public Task<GameListItemDto[]> FindGamesAsync()
        {
            var result = _gameStates.State.RegisteredGames
                .Select(x => new GameListItemDto() { Id = x.Key, XPlayerName = TryGetUserName(x.Value.XPlayer), OPlayerName = TryGetUserName(x.Value.OPlayer) })
                .ToArray();
            return Task.FromResult(result);
        }

        private string? TryGetUserName(AuthorizationToken? token) => token == null ? null : _userStates.State.AuthorizedUsers[token];

        /// <summary>
        /// TODO: add unit tests
        /// </summary>
        public async Task AddBotAsync(AuthorizationToken owner, GameId gameId)
        {
            var token = await AuthorizeAsync($"Bot (random)");
            var bot = GrainFactory.GetGrain<IGameBot>(gameId.Value);
            var gameData = _gameStates.State.RegisteredGames[gameId];
            var playForX = gameData.IsPlayingForX(token);
            await JoinGameAsync(token, gameId);
            await bot.InitAsync(token, playForX);
        }

        /// <summary>
        /// TODO: add unit tests
        /// </summary>
        public Task<string?[]> ResolveUserNamesAsync(params AuthorizationToken?[] tokens)
        {
            var result = tokens.Select(LookupUserName).ToArray();
            return Task.FromResult(result);
        }

        private string? LookupUserName(AuthorizationToken? token)
        {
            if (token == null)
            {
                return null;
            }
            var isKnownUser = _userStates.State.AuthorizedUsers.TryGetValue(token, out var result);
            if (!isKnownUser)
            {
                throw new ArgumentException($"User with token {token} is unknown");
            }
            return result;
        }
    }
}
