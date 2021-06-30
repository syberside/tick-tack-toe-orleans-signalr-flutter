using FluentAssertions;
using FluentAssertions.Execution;
using System;
using Xunit;

namespace OrleanPG.Grains.Interfaces.UnitTests
{
    public class GameMapPointUnitTests
    {
        [Theory]
        [InlineData(-1, 0)]
        [InlineData(GameMapPoint.MaxIndex + 1, 0)]
        [InlineData(0, -1)]
        [InlineData(0, GameMapPoint.MaxIndex + 1)]
        public void Ctor_OnIndexOutOfRange_Throws(int x, int y)
        {
            Action act = () => new GameMapPoint(x, y);

            act.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Fact]
        public void Ctor_OnValidArgs_DoesNotThrow()
        {
            var result = new GameMapPoint(1, 2);

            using (new AssertionScope())
            {
                result.X.Should().Be(1);
                result.Y.Should().Be(2);
            }
        }
    }
}