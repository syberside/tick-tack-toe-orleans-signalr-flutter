using OrleanPG.Grains.Interfaces.Infrastructure;
using System;
using System.ComponentModel;

namespace OrleanPG.Grains.Interfaces
{
    [TypeConverter(typeof(GameIdConverter))]
    public record GameId(Guid Value);

    public class GameIdConverter : TypeToStringConverter<GameId>
    {
        protected override string ConvertToString(GameId value) => value.Value.ToString();

        protected override GameId CreateFromString(string value) => new(Guid.Parse(value));
    }
}
