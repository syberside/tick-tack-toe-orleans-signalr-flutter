using OrleanPG.Grains.Interfaces;
using System;

namespace OrleanPG.Grains.Game.Engine
{
    public enum PlayerParticipation
    {
        X,
        O
    }

    public static class PlayerParticiptionExtensions
    {
        public static CellStatus ToCellStatus(this PlayerParticipation participaton)
        {
            switch (participaton)
            {
                case PlayerParticipation.X: return CellStatus.X;
                case PlayerParticipation.O: return CellStatus.O;
                default: throw new NotSupportedException();
            }
        }

        public static (PlayerParticipation, CellStatus marker) PlayerForState(GameStatus status)
        {
            switch (status)
            {
                case GameStatus.XTurn: return (PlayerParticipation.X, CellStatus.X);
                case GameStatus.OTurn: return (PlayerParticipation.O, CellStatus.O);
                default: throw new NotSupportedException();
            }
        }
    }
}
