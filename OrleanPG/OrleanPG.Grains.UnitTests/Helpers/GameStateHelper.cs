using OrleanPG.Grains.Interfaces;
using System.Linq;

namespace OrleanPG.Grains.UnitTests.Helpers
{
    public static class GameStateHelper
    {
        public static GameStatus AnyExceptThis(this GameStatus @this)
            => GameStatusExtension.Values.First(x => x != @this);

        public static GameStatus AnyFinalExpectThis(this GameStatus @this)
            => GameStatusExtension.Values.First(x => x != @this && x.IsEndStatus());

        public static GameStatus AnyNotFinalExpectThis(this GameStatus @this)
            => GameStatusExtension.Values.First(x => x != @this && !x.IsEndStatus());
    }
}
