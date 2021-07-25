using OrleanPG.Grains.Interfaces;

namespace OrleanPG.Grains.Game.Engine
{
    public class Win
    {
        public int Index { get; }

        public GameAxis Axis { get; }

        public Win(int index, GameAxis axis)
        {
            Index = index;
            Axis = axis;
        }
    }
}
