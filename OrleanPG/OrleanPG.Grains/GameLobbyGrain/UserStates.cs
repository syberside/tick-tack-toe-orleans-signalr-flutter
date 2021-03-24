using OrleanPG.Grains.Interfaces;
using System.Collections.Generic;

namespace OrleanPG.Grains.GameLobbyGrain
{
    public record UserStates(Dictionary<AuthorizationToken, string> AuthorizedUsers)
    {
        public UserStates() : this(new Dictionary<AuthorizationToken, string>()) { }
    }
}
