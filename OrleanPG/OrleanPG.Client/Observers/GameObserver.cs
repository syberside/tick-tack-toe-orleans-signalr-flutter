using OrleanPG.Grains.Interfaces;
using Orleans.Streams;
using System;

namespace OrleanPG.Client.Observers
{
    public class GameObserver
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

        internal void Clear()
        {
            IsUpdated = false;
            LastUpdate = null;
        }
    }
}
