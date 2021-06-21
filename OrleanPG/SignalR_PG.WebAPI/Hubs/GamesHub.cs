using Microsoft.AspNetCore.SignalR;
using OrleanPG.Grains.Interfaces;
using Orleans;
using SignalR_PG.WebAPI.Dto;
using SignalR_PG.WebAPI.Infrastructure;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SignalR_PG.WebAPI.Hubs
{
    public class GamesHub : Hub
    {
        private readonly IClusterClient _clusterClient;
        private readonly static ConcurrentDictionary<Guid, StreamToSignalRBridge> _subscriptions = new();

        public GamesHub(IClusterClient clusterClient)
        {
            _clusterClient = clusterClient;
        }

        public async Task<string> Login(string username)
        {
            var lobby = _clusterClient.GetGrain<IGameLobby>(Guid.Empty);
            var result = await lobby.AuthorizeAsync(username);
            return result.Value;
        }

        public async Task<GameGeneralInfoDto[]> GetLobbies()
        {
            //TODO: add subscription to games list update
            var lobby = _clusterClient.GetGrain<IGameLobby>(Guid.Empty);
            var result = await lobby.FindGamesAsync();
            return Convert(result);

        }

        private GameGeneralInfoDto[] Convert(GameListItemDto[] result)
        {
            return result.Select(x => new GameGeneralInfoDto
            {
                playerX = x.XPlayerName,
                playerO = x.OPlayerName,
                gameId = x.Id.Value.ToString(),
            }).ToArray();
        }

        public async Task<string> CreateGame(string authToken, bool playForX)
        {
            //TODO: add subscription to games list update
            var lobby = _clusterClient.GetGrain<IGameLobby>(Guid.Empty);
            var result = await lobby.CreateGameAsync(new AuthorizationToken(authToken), playForX);
            return result.Value.ToString();
        }

        public async Task Turn(int x, int y, string authToken, Guid gameId)
        {
            var lobby = _clusterClient.GetGrain<IGame>(gameId);
            var result = await lobby.TurnAsync(x, y, new AuthorizationToken(authToken));
        }

        public async Task AddBot(Guid gameId, string authenticationToken)
        {
            var game = _clusterClient.GetGrain<IGameLobby>(Guid.Empty);
            await game.AddBotAsync(new AuthorizationToken(authenticationToken), new GameId(gameId));
        }

        public async Task Watch(Guid gameId)
        {
            var groupName = GetGroupName(gameId);
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
            if (_subscriptions.ContainsKey(gameId))
            {
                return;
            }

            var streamProvider = _clusterClient.GetStreamProvider(Constants.GameUpdatesStreamProviderName);
            var stream = streamProvider.GetStream<GameStatusDto>(gameId, Constants.GameUpdatesStreamName);
            var subscriber = new StreamToSignalRBridge(stream, Clients.Group(groupName));
            await subscriber.Start();
        }

        private static string GetGroupName(Guid gameId) => $"Game: {gameId}";

        public async Task Unwatch(Guid gameId)
        {
            _subscriptions.Remove(gameId, out var subscr);
            await subscr.DisposeAsync();
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, GetGroupName(gameId));
        }
    }
}
