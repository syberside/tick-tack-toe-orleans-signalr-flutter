using AutoFixture.Xunit2;
using FluentAssertions;
using FluentAssertions.Execution;
using OrleanPG.Grains.Game.Engine;
using OrleanPG.Grains.Game.Engine.Actions;
using OrleanPG.Grains.Interfaces;
using System;
using Xunit;

namespace OrleanPG.Grains.UnitTests.Engine.Actions
{
    public class UserTurnActionUnitTests
    {
        [Theory]
        [InlineAutoData(-1, 0)]
        [InlineAutoData(GameMap.MaxIndex + 1, 0)]
        [InlineAutoData(0, -1)]
        [InlineAutoData(0, GameMap.MaxIndex + 1)]
        public void Ctor_OnIndexOutOfRange_Throws(int x, int y, PlayerParticipation participation)
        {
            Action act = () => new UserTurnAction(x, y, participation);

            act.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Theory]
        [AutoData]
        public void Ctor_OnValidArgs_DoesNotThrow(PlayerParticipation participation)
        {
            var result = new UserTurnAction(1, 2, participation);

            using (new AssertionScope())
            {
                result.X.Should().Be(1);
                result.Y.Should().Be(2);
                result.StepBy.Should().Be(participation);
            }
        }
    }
}
