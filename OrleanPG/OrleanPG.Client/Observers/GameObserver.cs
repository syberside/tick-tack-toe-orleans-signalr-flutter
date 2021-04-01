using OrleanPG.Grains.Interfaces;
using System;

namespace OrleanPG.Client.Observers
{
    public class GameObserver : IGameObserver
    {
        public bool IsUpdated { get; set; }
        public GameStatusDto LastUpdate { get; private set; }
        public void GameStateUpdated(GameStatusDto newState)
        {
            Console.WriteLine("Game udpated:");
            Console.WriteLine(newState.Status);
            Console.WriteLine(newState.GameMap);
            IsUpdated = true;
            LastUpdate = newState;
        }
    }
}
