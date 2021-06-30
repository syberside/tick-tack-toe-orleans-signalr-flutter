using System;
using System.Collections.Generic;
using System.Linq;

namespace OrleanPG.Grains.Interfaces
{
    public enum GameStatus
    {
        XTurn = 0,
        OTurn = 1,
        XWin = 2,
        OWin = 3,
        TimedOut = 4,
        Draw = 5,
    }

    public static class GameStatusExtension
    {
        public static bool IsEndStatus(this GameStatus status)
        {
            switch (status)
            {
                case GameStatus.XTurn:
                case GameStatus.OTurn:
                    return false;
                case GameStatus.XWin:
                case GameStatus.OWin:
                case GameStatus.TimedOut:
                case GameStatus.Draw:
                    return true;
                default:
                    throw new NotImplementedException();
            }
        }

        private static GameStatus[] _values = new[]
        {
            GameStatus.XTurn, GameStatus.XWin,
            GameStatus.OTurn, GameStatus.OWin,
            GameStatus.Draw, GameStatus.TimedOut,
        };

        public static IReadOnlyCollection<GameStatus> Values() => Array.AsReadOnly(_values);

        public static GameStatus AnyExceptThis(this GameStatus @this) => Values().First(x => x != @this);
    }
}
