using OrleanPG.Grains.Interfaces;
using System;

namespace OrleanPG.Grains.Game.Engine.Actions
{
    public record UserTurnAction : IGameAction
    {
        public int X { get; }

        public int Y { get; }

        public PlayerParticipation StepBy { get; }

        public UserTurnAction(int x, int y, PlayerParticipation participation)
        {
            if (0 > x || x > GameMap.MaxIndex)
            {
                throw new ArgumentOutOfRangeException(nameof(x), x, $"Should be positive and less than {GameMap.GameSize}");
            }
            if (0 > y || y > GameMap.MaxIndex)
            {
                throw new ArgumentOutOfRangeException(nameof(y), y, $"Should be positive and less than {GameMap.GameSize}");
            }

            X = x;
            Y = y;
            StepBy = participation;
        }
    }
}
