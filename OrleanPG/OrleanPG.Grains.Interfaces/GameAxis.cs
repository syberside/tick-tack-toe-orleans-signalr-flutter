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

        private static readonly IReadOnlyCollection<GameAxis> _asRo = Array.AsReadOnly(_values);

        public static IEnumerable<GameAxis> Values => _asRo;
    }
}
