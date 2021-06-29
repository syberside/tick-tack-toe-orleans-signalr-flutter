using System;

namespace OrleanPG.Grains.Game.Engine.Actions
{
    public class TimeOutAction : IGameAction
    {
        private TimeOutAction()
        {
        }

        private static Lazy<TimeOutAction> _timeoutLazy = new(() => new(), System.Threading.LazyThreadSafetyMode.ExecutionAndPublication);

        public static TimeOutAction Instance => _timeoutLazy.Value;
    }
}
