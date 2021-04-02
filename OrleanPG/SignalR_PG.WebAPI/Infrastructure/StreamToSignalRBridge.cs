using Microsoft.AspNetCore.SignalR;
using OrleanPG.Grains.Interfaces;
using Orleans.Streams;
using System;
using System.Threading.Tasks;

namespace SignalR_PG.WebAPI.Infrastructure
{
    public class StreamToSignalRBridge : IAsyncDisposable
    {
        private readonly IClientProxy _clientProxy;
        private readonly IAsyncStream<GameStatusDto> _stream;
        private StreamSubscriptionHandle<GameStatusDto> _handle;

        public StreamToSignalRBridge(IAsyncStream<GameStatusDto> from, IClientProxy to)
        {
            _clientProxy = to;
            _stream = from;
        }

        public async Task Start()
        {
            _handle = await _stream.SubscribeAsync(async (update, token) => await _clientProxy.SendAsync("GameUpdated", update));
        }


        public async ValueTask DisposeAsync()
        {
            if (_handle != null)
            {
                await _handle.UnsubscribeAsync();
            }
        }
    }
}
