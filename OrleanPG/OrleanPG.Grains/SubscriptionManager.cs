using Orleans.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OrleanPG.Grains
{
    /// <summary>
    /// TODO: Cover with tests
    /// </summary>
    public class SubscriptionManager<TSubscriber> where TSubscriber : IAddressable
    {
        private readonly Dictionary<TSubscriber, DateTime> _subscribers = new();
        private readonly Func<DateTime> _dateTimeProvider;

        public SubscriptionManager(Func<DateTime> dateTimeProvider)
        {
            _dateTimeProvider = dateTimeProvider;
        }

        public TimeSpan ExpirationTimeOut { get; set; } = TimeSpan.FromMinutes(1);

        public void SubscribeOrMarkAlive(TSubscriber subscriber) => _subscribers[subscriber ?? throw new ArgumentNullException()] = _dateTimeProvider();

        public void Unsubscribe(TSubscriber subscriber) => _subscribers.Remove(subscriber ?? throw new ArgumentNullException());

        public IReadOnlyCollection<TSubscriber> Subscribers
        {
            get
            {
                UnsubscribeExpired();
                return _subscribers.Keys.ToArray();
            }
        }

        public void UnsubscribeExpired()
        {
            var expiredAfter = _dateTimeProvider() - ExpirationTimeOut;
            var subscriptions = _subscribers.Where(x => x.Value < expiredAfter).Select(x => x.Key).ToArray();
            foreach (var subscription in subscriptions)
            {
                Unsubscribe(subscription);
            }
        }
    }
}
