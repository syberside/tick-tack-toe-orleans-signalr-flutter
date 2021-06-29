using System;
using System.Collections.Generic;
using System.Linq;

namespace OrleanPG.Grains.Interfaces
{
    public enum GameState
    {
        XTurn = 0,
        OTurn = 1,
        XWin = 2,
        OWin = 3,
        TimedOut = 4,
        Draw = 5,
    }

    public static class GameStateExtension
    {
        public static bool IsEndStatus(this GameState state)
        {
            switch (state)
            {
                case GameState.XTurn:
                case GameState.OTurn:
                    return false;
                case GameState.XWin:
                case GameState.OWin:
                case GameState.TimedOut:
                case GameState.Draw:
                    return true;
                default:
                    throw new NotImplementedException();
            }
        }

        private static GameState[] _values = new[]
        {
            GameState.XTurn, GameState.XWin,
            GameState.OTurn, GameState.OWin,
            GameState.Draw, GameState.TimedOut,
        };

        public static IReadOnlyCollection<GameState> Values() => Array.AsReadOnly(_values);

        public static GameState AnyExceptThis(this GameState @this) => Values().First(x => x != @this);
    }
}
