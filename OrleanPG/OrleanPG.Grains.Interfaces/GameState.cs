using System;

namespace OrleanPG.Grains.Interfaces
{
    public enum GameState
    {
        XTurn = 0,
        OTurn = 1,
        XWin = 2,
        OWin = 3,
        TimedOut = 4,
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
                    return true;
                default:
                    throw new NotImplementedException();
            }
        }
    }
}
