using OrleanPG.Grains.Interfaces.Dtos;
using System.Collections.Generic;

namespace OrleanPG.Grains.Interfaces
{
    /// <summary>
    /// NOTE:  `record` keyword can't be used as because this DTO is used in array
    /// </summary>
    public class GameListItemDto
    {
        // TODO: make nullable after upgrade to 3.5.0 (record types should be supported by that version)
#pragma warning disable CS8618 // Cant make this nullable because of strange behavior in generated Orleans code (typeof(GameId?))
        public GameId Id { get; init; }
#pragma warning restore CS8618
        public string? XPlayerName { get; init; }
        public string? OPlayerName { get; init; }
        public bool IsRunning => XPlayerName != null && OPlayerName != null;

        public override bool Equals(object? obj)
        {
            return obj is GameListItemDto dto &&
                   EqualityComparer<GameId>.Default.Equals(Id, dto.Id) &&
                   XPlayerName == dto.XPlayerName &&
                   OPlayerName == dto.OPlayerName;
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