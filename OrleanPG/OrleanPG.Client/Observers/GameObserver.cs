using OrleanPG.Grains.Interfaces;
using System;

namespace OrleanPG.Client.Observers
{
    public class GameObserver : IGameObserver
    {
        public void GameStateUpdated(GameStatusDto newState)
        {
            Console.WriteLine("Game udpated:");
            Console.WriteLine(newState.Status);
            Console.WriteLine(newState.GameMap);
        }
    }
}
