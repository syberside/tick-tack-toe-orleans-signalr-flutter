using OrleanPG.Grains.Interfaces;

namespace OrleanPG.Grains.Game.Engine.Actions
{
    public record UserTurnAction : IGameAction
    {
        public GameMapPoint Position { get; }

        public PlayerParticipation StepBy { get; }

        public UserTurnAction(GameMapPoint position, PlayerParticipation participation)
        {
            Position = position;
            StepBy = participation;
        }
    }
}
