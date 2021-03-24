using OrleanPG.Grains.Interfaces.Infrastructure;
using System.ComponentModel;

namespace OrleanPG.Grains.Interfaces
{
    [TypeConverter(typeof(AuthorizationTokenConverter))]
    public record AuthorizationToken(string Value);

    public class AuthorizationTokenConverter : TypeToStringConverter<AuthorizationToken>
    {
        protected override string ConvertToString(AuthorizationToken value) => value.Value;

        protected override AuthorizationToken CreateFromString(string value) => new AuthorizationToken(value);
    }
}
