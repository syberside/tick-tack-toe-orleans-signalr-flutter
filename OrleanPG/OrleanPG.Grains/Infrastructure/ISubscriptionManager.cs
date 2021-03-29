using Orleans.Runtime;
using System;
using System.Collections.Generic;

namespace OrleanPG.Grains.Infrastructure
{
    public interface ISubscriptionManager<TSubscriber> where TSubscriber : IAddressable
    {
        TimeSpan ExpirationTimeOut { get; set; }
        IReadOnlyCollection<TSubscriber> GetActualSubscribers { get; }
        IReadOnlyCollection<TSubscriber> GetAllSubscribers { get; }

        DateTime GetExpirationTime(TSubscriber subscriber);
        void SubscribeOrMarkAlive(TSubscriber subscriber);
        void Unsubscribe(TSubscriber subscriber);
        void UnsubscribeExpired();
    }
}