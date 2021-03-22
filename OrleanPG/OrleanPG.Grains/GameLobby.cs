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

        public Task<AuthorizationToken> AuthorizeAsync(string username)
        {
            var token = new AuthorizationToken(Guid.NewGuid().ToString());
            _userTokens[token] = username ?? throw new ArgumentNullException();
            return Task.FromResult(token);
        }

        public Task<CreateGameResult> CreateNewAsync(AuthorizationToken authToken, bool playForX)
        {
            ThrowIfUserTokenIsNotValid(authToken);
            var gameId = new GameId(Guid.NewGuid());
            var token = new GameToken(Guid.NewGuid().ToString());
            _gameTokens[gameId] = playForX ? new GameData(authToken, null, token) : new GameData(null, authToken, token);
            return Task.FromResult(new CreateGameResult(gameId, token));
        }

        private void ThrowIfUserTokenIsNotValid(AuthorizationToken authToken)
        {
            if (!_userTokens.ContainsKey(authToken))
            {
                throw new ArgumentException("User token is not valid");
            }
        }

        public Task<GameToken> JoinGameAsync(AuthorizationToken authToken, GameId id)
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
            var token = _gameTokens[id];
            if (token == null)
            {
                throw new ArgumentException($"Game not found: {id}");
            }

            _gameTokens[id] = token.JoinPlayer(authToken);
            return Task.FromResult(token.Token);
        }

        public Task<GameGeneralInfo[]> FindGamesAsync()
        {
            var result = _gameTokens
                .Select(x => new GameGeneralInfo() { Id = x.Key, XPlayerName = TryGetUserName(x.Value.XPlayer), OPlayerName = TryGetUserName(x.Value.OPlayer) })
                .ToArray();
            return Task.FromResult(result);
        }

        private string? TryGetUserName(AuthorizationToken? token) => token == null ? null : _userTokens[token];

        private record GameData(AuthorizationToken? XPlayer, AuthorizationToken? OPlayer, GameToken Token)
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
