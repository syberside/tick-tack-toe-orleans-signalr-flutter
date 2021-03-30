using Microsoft.AspNetCore.SignalR;
using OrleanPG.Grains.Interfaces;
using Orleans;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SignalR_PG.WebAPI.Hubs
{
    public class GamesHub : Hub
    {
        private readonly IClusterClient _clusterClient;
        private readonly static ConcurrentDictionary<Guid, Subscr> _subscriptions = new();

        public GamesHub(IClusterClient clusterClient)
        {
            _clusterClient = clusterClient;
        }

        public async Task Watch(Guid gameId)
        {
            var groupName = GetGroupName(gameId);
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
            if (_subscriptions.ContainsKey(gameId))
            {
                return;
            }

            var subscriber = new Subscr(Clients.Group(groupName));
            var subscrRef = await _clusterClient.CreateObjectReference<IGameObserver>(subscriber);
            var game = _clusterClient.GetGrain<IObservableGame>(gameId);
            await game.SubscribeAndMarkAlive(subscrRef);

        }

        private static string GetGroupName(Guid gameId) => $"Game: {gameId}";

        public async Task Ping(Guid gameId)
        {
            var game = _clusterClient.GetGrain<IObservableGame>(gameId);
            await game.SubscribeAndMarkAlive(_subscriptions[gameId]);
        }

        public async Task Unwatch(Guid gameId)
        {
            var game = _clusterClient.GetGrain<IObservableGame>(gameId);
            await game.UnsubscribeFromUpdates(_subscriptions[gameId]);
            _subscriptions.Remove(gameId, out var _);

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, GetGroupName(gameId));
        }
    }

    public class Subscr : IGameObserver
    {
        private readonly IClientProxy _clientProxy;

        public Subscr(IClientProxy clientProxy)
        {
            _clientProxy = clientProxy;
        }

        public async void GameStateUpdated(GameStatusDto newState)
        {
            await _clientProxy.SendAsync("GameUpdated", newState);
        }
    }
}
