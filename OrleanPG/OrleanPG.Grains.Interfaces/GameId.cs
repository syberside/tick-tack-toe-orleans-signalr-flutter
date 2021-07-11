using OrleanPG.Grains.Interfaces.Dtos;
using OrleanPG.Grains.Interfaces.Infrastructure;
using System;
using System.ComponentModel;

namespace OrleanPG.Grains.Interfaces.Dtos
{
    [TypeConverter(typeof(GameIdConverter))]
    public record GameId(Guid Value);
}

namespace OrleanPG.Grains.Interfaces
{
    public class GameIdConverter : TypeToStringConverter<GameId>
    {
        protected override string ConvertToString(GameId value) => value.Value.ToString();

        protected override GameId CreateFromString(string value) => new(Guid.Parse(value));
    }
}
