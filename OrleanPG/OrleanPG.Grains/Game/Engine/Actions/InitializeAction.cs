using OrleanPG.Grains.Interfaces;
using System;

namespace OrleanPG.Grains.Game.Engine.Actions
{
    public record InitializeAction : IGameAction
    {
        public AuthorizationToken XPlayer { get; }
        public AuthorizationToken OPlayer { get; }


        public InitializeAction(AuthorizationToken xPlayer, AuthorizationToken oPlayer)
        {
            XPlayer = xPlayer ?? throw new ArgumentNullException(nameof(xPlayer));
            OPlayer = oPlayer ?? throw new ArgumentNullException(nameof(oPlayer));
        }

    }
}
