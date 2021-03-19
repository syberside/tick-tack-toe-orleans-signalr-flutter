using Microsoft.Extensions.Logging;
using OrleanPG.Grains.Interfaces;
using Orleans;
using System;
using System.Threading.Tasks;

namespace OrleanPG.Grains
{
    public class TickTackToeGameHolder : Grain, ITickTackToeGameHolder
    {
        private readonly ILogger<TickTackToeGameHolder> _logger;

        public TickTackToeGameHolder(ILogger<TickTackToeGameHolder> logger)
        {
            _logger = logger;
        }

        public Task<string> MakeATurn(int x, int y, Guid userId)
        {
            var result = $"Received ({x};{y}) from {userId}";
            _logger.LogInformation(result);
            return Task.FromResult(result);
        }
    }
}
