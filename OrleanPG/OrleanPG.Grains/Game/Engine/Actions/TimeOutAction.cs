using System;

namespace OrleanPG.Grains.Game.Engine.Actions
{
    public class TimeOutAction : IGameAction
    {
        private TimeOutAction() { }

        public static TimeOutAction Instance => _timeoutLazy.Value;

        private static readonly Lazy<TimeOutAction> _timeoutLazy = new(() => new(), System.Threading.LazyThreadSafetyMode.ExecutionAndPublication);
    }
}
