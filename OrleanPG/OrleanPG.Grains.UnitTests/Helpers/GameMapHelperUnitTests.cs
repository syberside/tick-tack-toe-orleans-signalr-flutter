using FluentAssertions;
using Xunit;

namespace OrleanPG.Grains.UnitTests.Helpers
{
    public class GameMapHelperUnitTests
    {
        [Fact]
        public void ToMapString_Always_ReturnsCorrectString()
        {
            var map = new bool?[3, 3] {
                { null, true, false},
                { null, true, false},
                { null, true, false},
            };
            map.ToMapString().Should().Be("{null , true , false}\r\n{null , true , false}\r\n{null , true , false}\r\n");
        }
    }
}
