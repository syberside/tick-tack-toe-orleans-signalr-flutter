using System;
using System.Threading.Tasks;

namespace OrleanPG.Grains.Game.GrainLogic
{
    public class GrainReminderToReminderHandleAdaptor : IReminderHandle
    {
        private readonly Func<Task> _setCallback;
        private readonly Func<Task> _resetCallback;

        public GrainReminderToReminderHandleAdaptor(Func<Task> setCallback, Func<Task> resetCallback)
        {
            _setCallback = setCallback;
            _resetCallback = resetCallback;
        }


        public Task Reset() => _resetCallback();

        public Task Set() => _setCallback();
    }
}

