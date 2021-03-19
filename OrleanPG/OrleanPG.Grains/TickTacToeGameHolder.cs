using Microsoft.Extensions.Logging;
using OrleanPG.Grains.Interfaces;
using Orleans;
using System;
using System.Threading.Tasks;

namespace OrleanPG.Grains
{
    public class TickTacToeGameHolder : Grain, ITickTacToeGameHolder
    {
        private readonly ILogger<TickTacToeGameHolder> _logger;

        public TickTacToeGameHolder(ILogger<TickTacToeGameHolder> logger)
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
