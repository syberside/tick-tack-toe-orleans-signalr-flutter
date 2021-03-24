using OrleanPG.Grains.Interfaces;
using System;

namespace OrleanPG.Grains.GameLobbyGrain
{
    public record GameParticipation(AuthorizationToken? XPlayer, AuthorizationToken? OPlayer)
    {
        public bool IsRunning => XPlayer != null && OPlayer != null;

        public GameParticipation JoinPlayer(AuthorizationToken otherPlayer)
        {
            if (XPlayer == null)
            {
                if (otherPlayer == OPlayer)
                {
                    throw new ArgumentException();
                }
                return this with { XPlayer = otherPlayer };
            }
            else
            {
                if (otherPlayer == XPlayer)
                {
                    throw new ArgumentException();

                }
                return this with { OPlayer = otherPlayer };
            }
        }
    }

}
