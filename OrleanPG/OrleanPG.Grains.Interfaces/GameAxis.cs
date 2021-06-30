using System;
using System.Collections.Generic;

namespace OrleanPG.Grains.Interfaces
{
    public enum GameAxis
    {
        X,
        Y,
        MainDiagonal,
        SideDiagonal,
    }

    public static class GameAxisExtensions
    {
        private static readonly GameAxis[] _values = new[]
        {
            GameAxis.X, GameAxis.Y,
            GameAxis.MainDiagonal, GameAxis.SideDiagonal,
        };

        public static IEnumerable<GameAxis> Values { get; } = Array.AsReadOnly(_values);
    }
}
