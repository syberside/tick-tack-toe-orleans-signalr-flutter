using OrleanPG.Grains.Interfaces;
using Orleans;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OrleanPG.Grains
{
    public class GameLobby : Grain, IGameLobby
    {
        private readonly Dictionary<AuthorizationToken, string> _userTokens = new Dictionary<AuthorizationToken, string>();
        private readonly Dictionary<GameId, GameData> _gameTokens = new Dictionary<GameId, GameData>();

        /// <summary>
        /// Required for unit tests
        /// </summary>
        public new virtual IGrainFactory GrainFactory => base.GrainFactory;

        public Task<AuthorizationToken> AuthorizeAsync(string username)
        {
            var token = new AuthorizationToken(Guid.NewGuid().ToString());
            _userTokens[token] = username ?? throw new ArgumentNullException();
            return Task.FromResult(token);
        }

        public Task<GameId> CreateNewAsync(AuthorizationToken authToken, bool playForX)
        {
            ThrowIfUserTokenIsNotValid(authToken);
            var gameId = new GameId(Guid.NewGuid());
            _gameTokens[gameId] = playForX ? new GameData(authToken, null) : new GameData(null, authToken);
            return Task.FromResult(gameId);
        }

        private void ThrowIfUserTokenIsNotValid(AuthorizationToken authToken)
        {
            if (!_userTokens.ContainsKey(authToken))
            {
                throw new ArgumentException("User token is not valid");
            }
        }

        public async Task JoinGameAsync(AuthorizationToken authToken, GameId id)
        {
            ThrowIfUserTokenIsNotValid(authToken);
            if (id == null)
            {
                throw new ArgumentNullException();
            }
            if (!_gameTokens.ContainsKey(id))
            {
                throw new ArgumentException();
            }

            var gameData = _gameTokens[id];
            if (gameData == null)
            {
                throw new ArgumentException($"Game not found: {id}");
            }

            gameData = gameData.JoinPlayer(authToken);
            _gameTokens[id] = gameData;
            var init = GrainFactory.GetGrain<IGameInitializer>(id.Value);
            await init.StartAsync(gameData.XPlayer, gameData.OPlayer);
        }

        public Task<GameGeneralInfo[]> FindGamesAsync()
        {
            var result = _gameTokens
                .Select(x => new GameGeneralInfo() { Id = x.Key, XPlayerName = TryGetUserName(x.Value.XPlayer), OPlayerName = TryGetUserName(x.Value.OPlayer) })
                .ToArray();
            return Task.FromResult(result);
        }

        private string? TryGetUserName(AuthorizationToken? token) => token == null ? null : _userTokens[token];

        private record GameData(AuthorizationToken? XPlayer, AuthorizationToken? OPlayer)
        {
            public bool IsRunning => XPlayer != null && OPlayer != null;

            public GameData JoinPlayer(AuthorizationToken otherPlayer)
            {
                if (XPlayer == null)
                {
                    if (otherPlayer == OPlayer)
                    {
                        throw new ArgumentException();
                    }
                    return this with { XPlayer = otherPlayer };
                }
                else
                {
                    if (otherPlayer == XPlayer)
                    {
                        throw new ArgumentException();

                    }
                    return this with { OPlayer = otherPlayer };
                }
            }
        }

    }

}
