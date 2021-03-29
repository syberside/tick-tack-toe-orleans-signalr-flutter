using AutoFixture.Xunit2;
using FluentAssertions;
using OrleanPG.Grains.Infrastructure;
using Orleans.Runtime;
using System;
using Xunit;

namespace OrleanPG.Grains.UnitTests.Infrastructure
{
    public class SubscriptionManagerUnitTests
    {
        public class SubscriberMock : IAddressable { }

        private DateTime _nowMock = DateTime.Now;
        private readonly SubscriptionManager<SubscriberMock> _manager;
        public SubscriptionManagerUnitTests()
        {
            _manager = new SubscriptionManager<SubscriberMock>(() => _nowMock);
        }

        #region SubscribeOrMarkAlive
        [Fact]
        public void SubscribeOrMarkAlive_OnNull_Throws()
        {
            Action sut = () => _manager.SubscribeOrMarkAlive(null);
            sut.Should().Throw<ArgumentNullException>();
        }

        [Theory, AutoData]
        public void SubscribeOrMarkAlive_OnNotNull_AddsSubscriber(SubscriberMock subscriber)
        {
            _manager.SubscribeOrMarkAlive(subscriber);
            _manager.GetAllSubscribers.Should().BeEquivalentTo(new[] { subscriber });
        }


        [Theory, AutoData]
        public void SubscribeOrMarkAlive_OnNotNull_RefreshSubscriber(SubscriberMock subscriber, TimeSpan samplePeriod)
        {
            _manager.SubscribeOrMarkAlive(subscriber);
            _nowMock += samplePeriod;

            _manager.SubscribeOrMarkAlive(subscriber);

            _manager.GetExpirationTime(subscriber).Should().Be(_nowMock + _manager.ExpirationTimeOut);
        }
        #endregion

        [Fact]
        public void Unsubscribe_OnNull_Throws()
        {
            Action sut = () => _manager.Unsubscribe(null);
            sut.Should().Throw<ArgumentNullException>();
        }

        [Theory, AutoData]
        public void Unsubscribe_OnNotNull_RemovesSubscriber(SubscriberMock subscriber)
        {
            _manager.SubscribeOrMarkAlive(subscriber);
            _manager.Unsubscribe(subscriber);
            _manager.GetAllSubscribers.Should().BeEmpty();
        }

        [Theory, AutoData]
        public void GetActualSubscribers_Always_DoesntReturnsExpiredSubscribers(SubscriberMock subscriber)
        {
            _manager.SubscribeOrMarkAlive(subscriber);
            _nowMock += _manager.ExpirationTimeOut + TimeSpan.FromMilliseconds(1);

            _manager.GetActualSubscribers.Should().BeEmpty();
        }


        [Theory, AutoData]
        public void UnsubscribeExpired_Always_RemovesExpired(SubscriberMock subscriber)
        {
            _manager.SubscribeOrMarkAlive(subscriber);
            _nowMock += _manager.ExpirationTimeOut + TimeSpan.FromMilliseconds(1);

            _manager.UnsubscribeExpired();

            _manager.GetAllSubscribers.Should().BeEmpty();
        }

        #region GetExpirationTime
        [Theory, AutoData]
        public void GetExpirationTime_OnSubscribed_ReturnsCorrectValue(SubscriberMock subscriber, TimeSpan timeout)
        {
            _manager.SubscribeOrMarkAlive(subscriber);
            _manager.ExpirationTimeOut = timeout;

            var result = _manager.GetExpirationTime(subscriber);

            result.Should().Be(_nowMock + timeout);
        }
        #endregion
    }

}
