using OrleanPG.Grains.Interfaces;
using System;

namespace OrleanPG.Grains.GameLobbyGrain
{
    public record GameParticipation(AuthorizationToken? XPlayer, AuthorizationToken? OPlayer)
    {
        public bool IsRunning => XPlayer != null && OPlayer != null;

        public GameParticipation JoinPlayer(AuthorizationToken otherPlayer, out bool playForX)
        {
            if (XPlayer == null)
            {
                if (otherPlayer == OPlayer)
                {
                    throw new ArgumentException();
                }
                playForX = true;
                return this with { XPlayer = otherPlayer };
            }
            else
            {
                if (otherPlayer == XPlayer)
                {
                    throw new ArgumentException();

                }
                playForX = false;
                return this with { OPlayer = otherPlayer };
            }
        }

        public bool IsPlayingForX(AuthorizationToken token)
        {
            if (token == XPlayer)
            {
                return true;
            }
            if (token == OPlayer)
            {
                return false;
            }
            throw new ArgumentException($"Token {token} is not a part of game");
        }
    }

}
