using FluentAssertions;
using FluentAssertions.Execution;
using OrleanPG.Grains.Game.Engine;
using OrleanPG.Grains.Interfaces;
using Xunit;

namespace OrleanPG.Grains.Engine.UnitTests
{
    public class GameMapUnitTests
    {
        [Fact]
        public void HaveEmptyCells_OnNoEmptyCells_ReturnsFalse()
        {
            var data = new GameMap(new[,]
            {
                { CellStatus.X, CellStatus.O, CellStatus.X, },
                { CellStatus.O, CellStatus.O, CellStatus.X, },
                { CellStatus.X, CellStatus.O, CellStatus.X, },
            });
            data.HaveEmptyCells.Should().BeFalse();
        }

        [Fact]
        public void HaveEmptyCells_OnEmptyCells_ReturnsTrue()
        {
            var data = new GameMap(new[,]
            {
                { CellStatus.X, CellStatus.O,     CellStatus.X, },
                { CellStatus.O, CellStatus.Empty, CellStatus.X, },
                { CellStatus.X, CellStatus.O,     CellStatus.X, },
            });
            data.HaveEmptyCells.Should().BeTrue();
        }

        [Fact]
        public void HaveEmptyCells_OnEmptyCell_ReturnsTrue()
        {
            var data = new GameMap(new[,]
            {
                { CellStatus.Empty, CellStatus.Empty, CellStatus.Empty, },
                { CellStatus.Empty, CellStatus.Empty, CellStatus.Empty, },
                { CellStatus.Empty, CellStatus.Empty, CellStatus.Empty, },
            });
            data.HaveEmptyCells.Should().BeTrue();
        }

        [Fact]
        public void GetAvailableCells_OnEmptyCells_ReturnsAllEmptyCells()
        {
            var data = new GameMap(new[,]
            {
                { CellStatus.Empty, CellStatus.X, CellStatus.O, },
                { CellStatus.O, CellStatus.Empty, CellStatus.Empty, },
                { CellStatus.Empty, CellStatus.O, CellStatus.X, },
            });
            var result = data.GetAvailableCells();
            result.Should().BeEquivalentTo(new[]
            {
                new GameMapPoint(0,0), new GameMapPoint(1,1), new GameMapPoint(1,2), new GameMapPoint(2,0),
            });
        }

        [Fact]
        public void GetAvailableCells_OnNoEmptyCell_ReturnsEmpty()
        {
            var data = new GameMap(new[,]
            {
                { CellStatus.X, CellStatus.X, CellStatus.O, },
                { CellStatus.O, CellStatus.O, CellStatus.O, },
                { CellStatus.X, CellStatus.O, CellStatus.X, },
            });
            var result = data.GetAvailableCells();
            result.Should().BeEquivalentTo(new (int, int)[0]);

        }

        [Fact]
        public void EqualsAndGetHashCode_ForEqualItems_ReturnTrueAndSameHashCode()
        {
            var data1 = new GameMap(new[,]
            {
                { CellStatus.Empty, CellStatus.Empty, CellStatus.Empty, },
                { CellStatus.Empty, CellStatus.Empty, CellStatus.Empty, },
                { CellStatus.Empty, CellStatus.Empty, CellStatus.Empty, },
            });
            var data2 = new GameMap(new[,]
            {
                { CellStatus.Empty, CellStatus.Empty, CellStatus.Empty, },
                { CellStatus.Empty, CellStatus.Empty, CellStatus.Empty, },
                { CellStatus.Empty, CellStatus.Empty, CellStatus.Empty, },
            });
            using (new AssertionScope())
            {
                data1.Equals(data2).Should().BeTrue();
                data1.GetHashCode().Should().Be(data2.GetHashCode());
            }
        }

        [Fact]
        public void EqualsAndGetHashCode_ForNotEqualItems_ReturnFalseAnddifferentHashCode()
        {
            var data1 = new GameMap(new[,]
            {
                { CellStatus.Empty, CellStatus.Empty, CellStatus.Empty, },
                { CellStatus.Empty, CellStatus.Empty, CellStatus.Empty, },
                { CellStatus.Empty, CellStatus.Empty, CellStatus.Empty, },
            });
            var data2 = new GameMap(new[,]
            {
                { CellStatus.Empty, CellStatus.Empty, CellStatus.Empty, },
                { CellStatus.Empty, CellStatus.X, CellStatus.Empty, },
                { CellStatus.Empty, CellStatus.Empty, CellStatus.Empty, },
            });
            using (new AssertionScope())
            {
                data1.Equals(data2).Should().BeFalse();
                data1.GetHashCode().Should().NotBe(data2.GetHashCode());
            }
        }

        [Fact]
        public void Indexer_Always_AllowToSetAndGetValue()
        {
            var data = new GameMap(new[,]
            {
                { CellStatus.Empty, CellStatus.Empty, CellStatus.Empty, },
                { CellStatus.Empty, CellStatus.Empty, CellStatus.Empty, },
                { CellStatus.Empty, CellStatus.Empty, CellStatus.Empty, },
            });
            data = data.Update(new GameMapPoint(1, 1), CellStatus.X);
            data[new GameMapPoint(1, 1)].Should().Be(CellStatus.X);
            using (new AssertionScope())
            {
                for (var i = 0; i < GameMapPoint.GameSize; i++)
                {
                    for (var j = 0; j < GameMapPoint.GameSize; j++)
                    {
                        if (i == 1 && j == 1)
                        {
                            continue;
                        }
                        data[new GameMapPoint(i, j)].Should().Be(CellStatus.Empty);
                    }
                }
            }
        }
    }
}