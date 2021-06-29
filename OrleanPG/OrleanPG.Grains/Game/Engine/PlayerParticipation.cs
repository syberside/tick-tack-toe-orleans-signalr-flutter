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

        public static (PlayerParticipation, CellStatus marker) PlayerForState(GameState status)
        {
            switch (status)
            {
                case GameState.XTurn: return (PlayerParticipation.X, CellStatus.X);
                case GameState.OTurn: return (PlayerParticipation.O, CellStatus.O);
                default: throw new NotSupportedException();
            }
        }
    }
}
