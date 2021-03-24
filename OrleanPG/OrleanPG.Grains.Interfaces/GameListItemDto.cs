using System.Collections.Generic;

namespace OrleanPG.Grains.Interfaces
{
    /// <summary>
    /// NOTE:  `record` keyword can't be used as because this DTO is used in array
    /// </summary>
    public class GameListItemDto
    {
        public GameId Id { get; init; }
        public string? XPlayerName { get; init; }
        public string? OPlayerName { get; init; }
        public bool IsRunning => XPlayerName != null && OPlayerName != null;

        public override bool Equals(object? obj)
        {
            return obj is GameListItemDto info &&
                   EqualityComparer<GameId>.Default.Equals(Id, info.Id) &&
                   XPlayerName == info.XPlayerName &&
                   OPlayerName == info.OPlayerName;
        }

        public override int GetHashCode()
        {
            int hashCode = 1866609233;
            hashCode = hashCode * -1521134295 + EqualityComparer<GameId>.Default.GetHashCode(Id);
            hashCode = hashCode * -1521134295 + EqualityComparer<string?>.Default.GetHashCode(XPlayerName);
            hashCode = hashCode * -1521134295 + EqualityComparer<string?>.Default.GetHashCode(OPlayerName);
            return hashCode;
        }
    }
}