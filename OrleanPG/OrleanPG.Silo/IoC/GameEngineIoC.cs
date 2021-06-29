using Microsoft.Extensions.DependencyInjection;
using OrleanPG.Grains.Game.Engine;
using OrleanPG.Grains.Game.Engine.WinCheckers;

namespace OrleanPG.Silo.IoC
{
    public static class GameEngineIoC
    {
        public static IServiceCollection AddGameEngine(this IServiceCollection services)
        {
            return services
                .AddSingleton<IGameEngine, GameEngine>()
                .AddSingleton<IWinChecker, ByRowWinChecker>()
                .AddSingleton<IWinChecker, ByColWinChecker>()
                .AddSingleton<IWinChecker, ByMainDiagonalWinChecker>()
                .AddSingleton<IWinChecker, BySideDiagonalWinChecker>()
                ;
        }
    }
}
